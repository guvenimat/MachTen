using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;
using MACHTEN.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;

namespace MACHTEN.Infrastructure.Persistence;

/// <summary>
/// Holds the application's own entities plus the ASP.NET Core Identity and
/// OpenIddict schemas.
/// </summary>
public sealed class MachtenDbContext(DbContextOptions<MachtenDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registered explicitly rather than through DbContextOptions.UseOpenIddict().
        // That route goes through an IModelCustomizer, and a second library
        // installing its own displaces it — silently dropping OpenIddict's tables
        // from the migration with no error. It has happened here before.
        modelBuilder.UseOpenIddict();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MachtenDbContext).Assembly);
    }
}
