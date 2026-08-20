namespace MACHTEN.Application.Contracts.Persistence;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
