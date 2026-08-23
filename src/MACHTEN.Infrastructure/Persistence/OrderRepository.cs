using Dapper;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

[module: DapperAot]

namespace MACHTEN.Infrastructure.Persistence;

/// <summary>
/// Dapper.AOT rewrites these call sites at compile time, so the command and
/// materialisation code is generated rather than reflected over, and the SQL is
/// visible instead of being produced by a query translator.
///
/// The queries deliberately avoid the CommandDefinition overloads. Those are the
/// only way to hand Dapper a CancellationToken, but Dapper.AOT does not
/// recognise them (DapperLib/DapperAOT#112) - it does not even count them as
/// candidates, so the interceptor silently does nothing and the reflection path
/// runs instead. That cost a debugging session here: clean build, green tests,
/// and none of it actually AOT.
///
/// Cancellation is honoured while acquiring the connection, which is where a
/// request is most likely to be waiting anyway.
/// </summary>
public sealed class OrderRepository(IConfiguration configuration) : IOrderRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    public async Task InsertAsync(Order order, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            INSERT INTO Orders (Id, CustomerReference, TotalAmount, TotalCurrency, Status, CreatedAtUtc)
            VALUES (@Id, @CustomerReference, @TotalAmount, @TotalCurrency, @Status, @CreatedAtUtc);
            """,
            new OrderRow(
                order.Id,
                order.CustomerReference,
                order.Total.Amount,
                order.Total.Currency,
                (int)order.Status,
                order.CreatedAtUtc));
    }

    public async Task<OrderDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<OrderRow>(
            """
            SELECT Id, CustomerReference, TotalAmount, TotalCurrency, Status, CreatedAtUtc
            FROM Orders
            WHERE Id = @Id;
            """,
            new { Id = id });

        if (row is null)
        {
            return null;
        }

        // Rebuild the value object rather than re-implementing its formatting
        // here. That string is user-visible and already cost one culture bug;
        // it should have exactly one definition.
        var total = new Money(row.TotalAmount, row.TotalCurrency);

        return new OrderDto(
            row.Id,
            row.CustomerReference,
            total.Amount,
            total.Currency,
            total.Formatted,
            ((OrderStatus)row.Status).ToString(),
            row.CreatedAtUtc);
    }

    /// <summary>Flat column shape; Dapper needs settable members, the aggregate has none.</summary>
    public sealed record OrderRow(
        Guid Id,
        string CustomerReference,
        decimal TotalAmount,
        string TotalCurrency,
        int Status,
        DateTimeOffset CreatedAtUtc);
}
