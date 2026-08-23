using MACHTEN.Domain.Entities;
using MACHTEN.Infrastructure.Persistence.Schema;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Infrastructure.Persistence;

/// <summary>
/// EF Core is kept only to own the schema and generate migrations — every
/// runtime read and write goes through Dapper.AOT. That is why the entities
/// here are flat row shapes rather than the domain model.
///
/// The last step of the AOT migration replaces this with plain SQL scripts.
/// </summary>
public sealed class MachtenDbContext(DbContextOptions<MachtenDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<UserRow> Users => Set<UserRow>();

    public DbSet<ApiClientRow> ApiClients => Set<ApiClientRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MachtenDbContext).Assembly);
    }
}
