using MACHTEN.Application.Contracts.Auth;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Api.Infrastructure.Auth;

/// <summary>
/// Applies migrations and seeds a demo client and user so the template is
/// usable immediately. Guard this behind Development (or drop it) before
/// deploying anywhere real.
/// </summary>
public sealed class AuthSeeder(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        await provider.GetRequiredService<MachtenDbContext>().Database.MigrateAsync(ct);

        var hasher = provider.GetRequiredService<IPasswordHasher<string>>();

        var clientId = configuration["Auth:Seed:ClientId"]!;
        var clients = provider.GetRequiredService<IApiClientRepository>();

        if (await clients.FindAsync(clientId, ct) is null)
        {
            // Secrets are hashed exactly like passwords — a leaked table should
            // not hand over working credentials.
            await clients.InsertAsync(
                new ApiClient(
                    clientId,
                    hasher.HashPassword(clientId, configuration["Auth:Seed:ClientSecret"]!),
                    "api"),
                ct);
        }

        var email = configuration["Auth:Seed:UserEmail"]!;
        var users = provider.GetRequiredService<IUserRepository>();

        if (await users.FindByEmailAsync(email, ct) is null)
        {
            await users.InsertAsync(
                new AuthUser(
                    Guid.CreateVersion7(),
                    email,
                    hasher.HashPassword(email, configuration["Auth:Seed:UserPassword"]!)),
                ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
