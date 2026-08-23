using System.Security.Cryptography;
using ZLogger;
using System.Threading.RateLimiting;
using Microsoft.Data.SqlClient;
using FastEndpoints;
using FastEndpoints.Swagger;
using Scalar.AspNetCore;
using MACHTEN.Api;
using MACHTEN.Api.Features.Auth;
using MACHTEN.Api.Infrastructure.Auth;
using MACHTEN.Api.Infrastructure.Errors;
using MACHTEN.Api.Infrastructure.Logging;
using MACHTEN.Api.Infrastructure.Observability;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Application.Contracts.Auth;
using MACHTEN.Infrastructure.Identity;
using MACHTEN.Infrastructure.Caching;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using JasperFx.CodeGeneration.Model;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Application.Features.Orders.PlaceOrder;
using MACHTEN.Domain.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

// Container HEALTHCHECK path: probe /health and exit, without starting a host.
if (args.Contains(HealthCheckProbe.Argument))
{
    return await HealthCheckProbe.RunAsync(new ConfigurationBuilder().AddEnvironmentVariables().Build());
}

var builder = WebApplication.CreateBuilder(args);

// ── Logging (ZLogger) ──
// Replaces the default console provider. ZLogger writes UTF8 directly and its
// source generator formats at compile time, so a log call allocates nothing on
// the hot path. IncludeScopes is required or the correlation id that
// CorrelationIdMiddleware puts in scope never reaches the output.
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options =>
{
    options.IncludeScopes = true;

    // JSON in every environment, including development. The plain text
    // formatter ignores scope values even with IncludeScopes on, so the
    // correlation id would reach the response header and never the logs -
    // which defeats the point of having it. Pipe through `jq` to read locally.
    options.UseJsonFormatter(formatter =>
        formatter.IncludeProperties =
            IncludeProperties.Timestamp |
            IncludeProperties.LogLevel |
            IncludeProperties.CategoryName |
            IncludeProperties.Message |
            IncludeProperties.Exception |
            IncludeProperties.ParameterKeyValues |
            IncludeProperties.ScopeKeyValues);
});

// ── EF Core ──
builder.Services.AddDbContextPool<MachtenDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Orders are read and written with Dapper.AOT rather than EF Core. EF still
// owns the Identity and OpenIddict schemas plus migrations; see NOTES.md.
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ── Auth: users, clients and token issuance ──
// OpenIddict and ASP.NET Core Identity are gone; their stores were EF Core and
// EF is on its way out. What replaces them is deliberately small: two tables
// read with Dapper, PBKDF2 hashing from Microsoft's PasswordHasher, and tokens
// signed with the key below. No discovery document, no refresh flow.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();
builder.Services.AddSingleton<IPasswordHasher<string>, PasswordHasher<string>>();

// Signing key shared with the JWT Bearer handler, so validation needs no
// metadata discovery - which is also what makes it work under
// WebApplicationFactory.
// Deliberately not disposed: it must outlive every token it signs.
#pragma warning disable CA2000
var rsa = RSA.Create();
#pragma warning restore CA2000
rsa.ImportRSAPrivateKey(Convert.FromBase64String(builder.Configuration["Auth:SigningKey"]!), out _);
var signingKey = new RsaSecurityKey(rsa);
var issuer = new Uri(builder.Configuration["Auth:Issuer"]!);

builder.Services.AddSingleton<ITokenIssuer>(_ => new TokenIssuer(
    new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
    issuer.AbsoluteUri));

// ── JWT Bearer (resource server) ──
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = false;

        // Keep OpenIddict's claim names ("sub", "name") instead of letting the
        // handler rewrite them to the legacy WS-Federation URIs.
        opts.MapInboundClaims = false;

        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer.AbsoluteUri,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateAudience = false,
            ValidateLifetime = true,
            NameClaimType = ClaimNames.Name,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// Applies migrations and seeds the demo client/user (Development only).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<AuthSeeder>();
}

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
})
// Cached types are listed explicitly, the same way SerializerContext lists
// its JSON types. Anything not named here falls back to HybridCache's JSON.
.AddSerializer<OrderDto, MemoryPackCacheSerializer<OrderDto>>();
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

    opts.OnRejected = (context, _) =>
    {
        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter")
            .LogRateLimitExceeded(context.HttpContext.Request.Path);

        return ValueTask.CompletedTask;
    };
    // Applied by name on the endpoints that need it (see PlaceOrderEndpoint).
    // A policy nothing references is just configuration theatre.
    //
    // Partitioned per caller rather than global: one noisy client should not be
    // able to spend everyone else's budget.
    opts.AddPolicy("writes", httpContext =>
    {
        var partitionKey =
            httpContext.Request.Headers["X-Client-Id"].FirstOrDefault()
            ?? httpContext.User.FindFirst(ClaimNames.Subject)?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),

            // No queue: holding a write request for up to a full window is
            // worse for the caller than a 429 it can retry on. It also keeps
            // the limiter's behaviour observable instead of just slow.
            QueueLimit = 0
        });
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
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// ── FastEndpoints ──
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    // Required when endpoints declare Version(n): without it FastEndpoints
    // filters every versioned endpoint out and emits a document with schemas
    // but no paths.
    o.MaxEndpointVersion = 1;
    o.DocumentSettings = s =>
    {
        s.Title = "MACHTEN API";
        s.DocumentName = "v1";
        s.Version = "v1";
    };
});

// ── Wolverine: handlers and transactional outbox ──
builder.Host.UseWolverine(opts =>
{
    // Handlers live in the Application layer; Wolverine only scans the entry
    // assembly by default and would silently find none.
    opts.Discovery.IncludeAssembly(typeof(PlaceOrderCommand).Assembly);

    // IApplicationDbContext is registered as a lambda so it hands back the same
    // pooled MachtenDbContext instance. Wolverine 6 cannot read through that to
    // generate a direct constructor call, and disallows service location by
    // default. Allowing it costs one container lookup per message and keeps both
    // the pooling and the layering intact.
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

    // Durable outbox on the same SQL Server the app already uses: an
    // OrderPlaced message is written inside the order's transaction, so it can
    // never be published for an order that rolled back, nor lost after commit.
    opts.PersistMessagesWithSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        "wolverine");

    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();

    // OrderPlaced goes to a durable local queue. The outbox guarantee is the
    // same as with an external broker - the message is written in the order's
    // transaction and delivered after commit - but it needs no extra
    // infrastructure, so it can actually be tested. Swapping in a real broker
    // is a transport line here, not a change to the publishing code.
    opts.PublishMessage<OrderPlaced>()
        .ToLocalQueue("orders-placed")
        .UseDurableInbox();
});

var app = builder.Build();

// First in the pipeline so even failures are traceable.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Versioning.Prefix = "v";
    c.Versioning.PrependToRoute = true;
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    c.Serializer.Options.TypeInfoResolverChain.Insert(0, AppSerializerContext.Default);
});
app.MapTokenEndpoint();
// NSwag still generates the OpenAPI document — it is what understands the
// FastEndpoints endpoints — but the Swagger UI is replaced by Scalar. The
// document path is set explicitly to the pattern Scalar expects by default.
// NSwag still builds the OpenAPI document — it is what understands the
// FastEndpoints endpoints — but UseOpenApi serves only the JSON, with no
// Swagger UI. Scalar renders it, and reads this path by default.
app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference(o => o.WithTitle("MACHTEN API"));
app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");

app.Run();

return 0;

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
