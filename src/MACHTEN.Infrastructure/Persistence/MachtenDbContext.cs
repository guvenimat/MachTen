using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Infrastructure.Persistence;

public sealed class MachtenDbContext(DbContextOptions<MachtenDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
