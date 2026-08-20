using System.Threading.RateLimiting;
using Microsoft.Data.SqlClient;
using FastEndpoints;
using FastEndpoints.Swagger;
using MACHTEN.Api;
using MACHTEN.Api.Infrastructure.Errors;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core ──
builder.Services.AddDbContextPool<MachtenDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<MachtenDbContext>());

// ── Background jobs (TickerQ) ──
// Jobs are discovered by source generator, not reflection. The operational
// store keeps schedules and run history in the app database; UseModelCustomizer
// wires its schema in at design time only, so the runtime model stays clean.
builder.Services.AddTickerQ(opts =>
    opts.AddOperationalStore(efOpts =>
        efOpts.UseApplicationDbContext<MachtenDbContext>(ConfigurationType.UseModelCustomizer)));

// ── Caching: L2 distributed store (Microsoft Garnet) ──
builder.Services.AddStackExchangeRedisCache(opts =>
{
    opts.Configuration = builder.Configuration.GetConnectionString("Cache");
    opts.InstanceName = "machten:";
});

// ── HybridCache: L1 in-process + L2 distributed ──
builder.Services.AddHybridCache(opts =>
{
    opts.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddSingleton<MACHTEN.Application.Contracts.ICacheStore, MACHTEN.Infrastructure.Caching.CacheStore>();

// ── Global Exception Handling ──
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Health Checks ──
// Checks against the server (via master), not the app database — the app database
// may not exist yet until the first EF Core migration is applied.
var sqlServerConnection = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
{
    InitialCatalog = "master"
}.ConnectionString;

builder.Services.AddHealthChecks()
    .AddSqlServer(sqlServerConnection, name: "sqlserver")
    .AddRedis(builder.Configuration.GetConnectionString("Cache")!, name: "cache");

// ── Rate Limiting ──
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddFixedWindowLimiter("fixed", window =>
    {
        window.PermitLimit = 100;
        window.Window = TimeSpan.FromMinutes(1);
        window.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        window.QueueLimit = 10;
    });
});

// ── OpenTelemetry ──
var otelResource = ResourceBuilder.CreateDefault().AddService("MACHTEN.Api");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// ── FastEndpoints ──
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "MACHTEN API";
        s.Version = "v1";
    };
});

// ── Wolverine ──
builder.Host.UseWolverine();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Versioning.Prefix = "v";
    c.Versioning.PrependToRoute = true;
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    c.Serializer.Options.TypeInfoResolverChain.Insert(0, AppSerializerContext.Default);
});
app.UseSwaggerGen();
app.MapPrometheusScrapingEndpoint();
app.UseTickerQ();
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
