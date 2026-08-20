using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;

namespace MACHTEN.Infrastructure.Persistence;

/// <summary>
/// Holds the application's own entities plus the ASP.NET Core Identity and
/// OpenIddict schemas. TickerQ's tables are added separately by its model
/// customizer at design time.
/// </summary>
public sealed class MachtenDbContext(DbContextOptions<MachtenDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registered explicitly rather than through DbContextOptions.UseOpenIddict():
        // TickerQ installs its own IModelCustomizer, which would otherwise displace
        // OpenIddict's and silently drop its tables from migrations.
        modelBuilder.UseOpenIddict();

        // Register application entities here as features are added, e.g.:
        // modelBuilder.Entity<MyEntity>(entity => { ... });
    }
}
