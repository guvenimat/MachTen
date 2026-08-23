using Dapper;
using MACHTEN.Application.Contracts.Auth;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MACHTEN.Infrastructure.Identity;

/// <summary>
/// Users and API clients read with Dapper.AOT. Simple overloads only — the
/// CommandDefinition ones are not intercepted (see NOTES.md).
/// </summary>
public sealed class UserRepository(IConfiguration configuration) : IUserRepository
{
    private readonly string _connectionString = AuthConnection.From(configuration);

    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<AuthUser>(
            "SELECT Id, Email, PasswordHash FROM Users WHERE Email = @Email;",
            new { Email = email });
    }

    public async Task InsertAsync(AuthUser user, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            "INSERT INTO Users (Id, Email, PasswordHash) VALUES (@Id, @Email, @PasswordHash);",
            user);
    }
}

public sealed class ApiClientRepository(IConfiguration configuration) : IApiClientRepository
{
    private readonly string _connectionString = AuthConnection.From(configuration);

    public async Task<ApiClient?> FindAsync(string clientId, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<ApiClient>(
            "SELECT ClientId, SecretHash, Scopes FROM ApiClients WHERE ClientId = @ClientId;",
            new { ClientId = clientId });
    }

    public async Task InsertAsync(ApiClient client, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            "INSERT INTO ApiClients (ClientId, SecretHash, Scopes) VALUES (@ClientId, @SecretHash, @Scopes);",
            client);
    }
}

internal static class AuthConnection
{
    public static string From(IConfiguration configuration) =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
}
