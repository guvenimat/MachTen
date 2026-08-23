using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MACHTEN.Infrastructure.Persistence.Schema;

/// <summary>Schema-only shapes: EF creates these tables, Dapper reads them.</summary>
public sealed class UserRow
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed class ApiClientRow
{
    public string ClientId { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
}

public sealed class UserRowConfiguration : IEntityTypeConfiguration<UserRow>
{
    public void Configure(EntityTypeBuilder<UserRow> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public sealed class ApiClientRowConfiguration : IEntityTypeConfiguration<ApiClientRow>
{
    public void Configure(EntityTypeBuilder<ApiClientRow> builder)
    {
        builder.ToTable("ApiClients");
        builder.HasKey(c => c.ClientId);
        builder.Property(c => c.ClientId).HasMaxLength(128);
        builder.Property(c => c.SecretHash).HasMaxLength(512).IsRequired();
        builder.Property(c => c.Scopes).HasMaxLength(512).IsRequired();
    }
}
