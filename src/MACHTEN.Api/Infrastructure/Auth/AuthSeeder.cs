using MACHTEN.Infrastructure.Identity;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MACHTEN.Api.Infrastructure.Auth;

/// <summary>
/// Applies migrations and seeds a demo client and user so the template is
/// usable immediately. Guard this behind the Development environment (or drop
/// it) before deploying anywhere real.
/// </summary>
public sealed class AuthSeeder(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<MachtenDbContext>();
        await db.Database.MigrateAsync(ct);

        await SeedClientAsync(scope.ServiceProvider, ct);
        await SeedUserAsync(scope.ServiceProvider, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedClientAsync(IServiceProvider provider, CancellationToken ct)
    {
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();

        var clientId = configuration["Auth:Seed:ClientId"]!;
        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
            return;

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = configuration["Auth:Seed:ClientSecret"],
            DisplayName = "MachTen sample client",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Prefixes.Scope + "api"
            }
        }, ct);
    }

    private async Task SeedUserAsync(IServiceProvider provider, CancellationToken ct)
    {
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["Auth:Seed:UserEmail"]!;
        if (await users.FindByNameAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await users.CreateAsync(user, configuration["Auth:Seed:UserPassword"]!);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed the demo user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
