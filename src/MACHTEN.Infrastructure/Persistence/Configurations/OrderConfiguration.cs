using MACHTEN.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MACHTEN.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerReference)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<int>();

        // Money is a value object, so it maps into the Order row rather than a
        // table of its own. Formatted is derived, so it is not persisted.
        builder.ComplexProperty(o => o.Total, total =>
        {
            total.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("TotalAmount");
            total.Property(m => m.Currency).HasMaxLength(3).HasColumnName("TotalCurrency");
            total.Ignore(m => m.Formatted);
        });

        builder.Ignore(o => o.DomainEvents);

        builder.HasIndex(o => o.CustomerReference);
    }
}
