using MACHTEN.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Application.Contracts.Persistence;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
