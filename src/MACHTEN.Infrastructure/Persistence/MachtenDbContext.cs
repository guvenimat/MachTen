using MACHTEN.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Infrastructure.Persistence;

public sealed class MachtenDbContext(DbContextOptions<MachtenDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Register entities here as features are added, e.g.:
        // modelBuilder.Entity<MyEntity>(entity => { ... });
    }
}
