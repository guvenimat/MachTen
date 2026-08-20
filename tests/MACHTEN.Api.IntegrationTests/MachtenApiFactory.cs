using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace MACHTEN.Api.IntegrationTests;

public sealed class MachtenApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7").Build();

    /// <summary>Credentials the AuthSeeder registers on startup.</summary>
    public const string ClientId = "machten-sample-client";
    public const string ClientSecret = "machten-sample-secret";
    public const string UserEmail = "demo@machten.local";
    public const string UserPassword = "Demo_P@ssw0rd!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development runs AuthSeeder, which migrates the container database and
        // registers the demo client/user the auth tests sign in with.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
            [
                new("ConnectionStrings:DefaultConnection", _sqlContainer.GetConnectionString()),
                new("ConnectionStrings:Cache", _redisContainer.GetConnectionString()),

                // Blank disables the Kafka transport (Program.cs treats an empty
                // value as "not configured"). Messages still flow through the
                // durable outbox; OutboxTests spins up a real broker separately.
                new("ConnectionStrings:Kafka", string.Empty)
            ]);
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Set as environment variables, not just through ConfigureAppConfiguration.
        //
        // Program.cs reads some connection strings eagerly - Wolverine's message
        // store is configured before builder.Build() - and ConfigureAppConfiguration
        // contributions are not applied until the host is built. Those eager reads
        // would otherwise see appsettings.json ("Server=localhost", named pipes)
        // and fail to reach the container.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection", _sqlContainer.GetConnectionString());
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Cache", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Kafka", string.Empty);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
