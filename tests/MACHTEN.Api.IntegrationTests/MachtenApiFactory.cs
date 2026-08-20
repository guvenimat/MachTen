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
                new("ConnectionStrings:Cache", _redisContainer.GetConnectionString())
            ]);
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
