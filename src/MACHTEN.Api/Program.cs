using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.Data.SqlClient;
using FastEndpoints;
using FastEndpoints.Swagger;
using MACHTEN.Api;
using MACHTEN.Api.Features.Auth;
using MACHTEN.Api.Infrastructure.Auth;
using MACHTEN.Api.Infrastructure.Errors;
using MACHTEN.Api.Infrastructure.Observability;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Infrastructure.Identity;
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
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using MACHTEN.Domain.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Kafka;
using Wolverine.SqlServer;
using static OpenIddict.Abstractions.OpenIddictConstants;

// Container HEALTHCHECK path: probe /health and exit, without starting a host.
if (args.Contains(HealthCheckProbe.Argument))
{
    return await HealthCheckProbe.RunAsync(new ConfigurationBuilder().AddEnvironmentVariables().Build());
}

var builder = WebApplication.CreateBuilder(args);

// ── EF Core ──
builder.Services.AddDbContextPool<MachtenDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    opts.UseOpenIddict();
});
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<MachtenDbContext>());

// ── Identity ──
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
    {
        opts.Password.RequiredLength = 8;
        opts.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<MachtenDbContext>()
    .AddDefaultTokenProviders();

// Emit the claim types OpenIddict expects rather than the ASP.NET defaults.
builder.Services.Configure<IdentityOptions>(opts =>
{
    opts.ClaimsIdentity.UserIdClaimType = Claims.Subject;
    opts.ClaimsIdentity.UserNameClaimType = Claims.Name;
    opts.ClaimsIdentity.RoleClaimType = Claims.Role;
});

// ── OpenIddict (authorization server) ──
// Keys come from configuration rather than development certificates, so every
// instance signs with the same material and token validation needs no metadata
// discovery round-trip (which also makes it work under WebApplicationFactory).
// OpenIddict signs JWT access tokens with an asymmetric key; the JWT Bearer
// handler below validates using the public half of this same key.
// Deliberately not disposed: the key must stay alive for the lifetime of the
// application, since OpenIddict signs every token with it.
#pragma warning disable CA2000
var rsa = RSA.Create();
#pragma warning restore CA2000
rsa.ImportRSAPrivateKey(Convert.FromBase64String(builder.Configuration["Auth:SigningKey"]!), out _);
var signingKey = new RsaSecurityKey(rsa);
var encryptionKey = new SymmetricSecurityKey(
    Convert.FromBase64String(builder.Configuration["Auth:EncryptionKey"]!));
var issuer = new Uri(builder.Configuration["Auth:Issuer"]!);

builder.Services.AddOpenIddict()
    .AddCore(opts => opts
        .UseEntityFrameworkCore()
        .UseDbContext<MachtenDbContext>())
    .AddServer(opts =>
    {
        opts.SetTokenEndpointUris("connect/token");
        opts.SetIssuer(issuer);

        opts.AllowClientCredentialsFlow()
            .AllowPasswordFlow()
            .AllowRefreshTokenFlow();

        opts.AddSigningKey(signingKey);
        opts.AddEncryptionKey(encryptionKey);

        // Access tokens stay readable so the JWT Bearer handler can validate
        // them; refresh tokens remain encrypted.
        opts.DisableAccessTokenEncryption();

        opts.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Roles, "api");

        var aspNetCore = opts.UseAspNetCore().EnableTokenEndpointPassthrough();

        // OpenIddict refuses plaintext HTTP by default. Only relax that outside
        // production, where TLS terminates in front of the app.
        if (!builder.Environment.IsProduction())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    });

// ── JWT Bearer (resource server) ──
// AddIdentity above points the default challenge at its cookie handler, which
// would redirect unauthenticated API callers to a login page that does not
// exist (404 instead of 401). Pin every default back to bearer tokens.
builder.Services.AddAuthentication(opts =>
    {
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
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
            NameClaimType = Claims.Name,
            RoleClaimType = Claims.Role
        };
    });

builder.Services.AddAuthorization();

// Applies migrations and seeds the demo client/user (Development only).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<AuthSeeder>();
}

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
    // Applied by name on the endpoints that need it (see PlaceOrderEndpoint).
    // A policy nothing references is just configuration theatre.
    //
    // Partitioned per caller rather than global: one noisy client should not be
    // able to spend everyone else's budget.
    opts.AddPolicy("writes", httpContext =>
    {
        var partitionKey =
            httpContext.Request.Headers["X-Client-Id"].FirstOrDefault()
            ?? httpContext.User.FindFirst(Claims.Subject)?.Value
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
    o.DocumentSettings = s =>
    {
        s.Title = "MACHTEN API";
        s.Version = "v1";
    };
});

// ── Wolverine: handlers, transactional outbox, Kafka ──
builder.Host.UseWolverine(opts =>
{
    // Durable outbox on the same SQL Server the app already uses: an
    // OrderPlaced message is written inside the order's transaction, so it can
    // never be published for an order that rolled back, nor lost after commit.
    opts.PersistMessagesWithSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        "wolverine");

    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();

    var kafka = builder.Configuration.GetConnectionString("Kafka");
    if (!string.IsNullOrWhiteSpace(kafka))
    {
        opts.UseKafka(kafka).AutoProvision();

        opts.PublishMessage<OrderPlaced>()
            .ToKafkaTopic("machten.orders.placed")
            .UseDurableOutbox();
    }
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
app.UseSwaggerGen();
app.MapPrometheusScrapingEndpoint();
app.UseTickerQ();
app.MapHealthChecks("/health");

app.Run();

return 0;

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
