using MACHTEN.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Application.Contracts.Persistence;

public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
