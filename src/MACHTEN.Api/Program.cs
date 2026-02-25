using System.Threading.RateLimiting;
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
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core ──
builder.Services.AddDbContextPool<MachtenDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<MachtenDbContext>());

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
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    c.Serializer.Options.TypeInfoResolverChain.Insert(0, AppSerializerContext.Default);
});
app.UseSwaggerGen();
app.MapPrometheusScrapingEndpoint();

app.Run();
