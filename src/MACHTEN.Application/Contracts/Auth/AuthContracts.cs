namespace MACHTEN.Application.Contracts.Auth;

/// <summary>A user who can exchange a password for a token.</summary>
public sealed record AuthUser(Guid Id, string Email, string PasswordHash);

/// <summary>A machine client that can exchange a secret for a token.</summary>
public sealed record ApiClient(string ClientId, string SecretHash, string Scopes);

public interface IUserRepository
{
    Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task InsertAsync(AuthUser user, CancellationToken ct = default);
}

public interface IApiClientRepository
{
    Task<ApiClient?> FindAsync(string clientId, CancellationToken ct = default);

    Task InsertAsync(ApiClient client, CancellationToken ct = default);
}

/// <summary>
/// Issues signed access tokens. Kept behind an interface so the Application
/// layer never sees the signing key or the JWT library.
/// </summary>
public interface ITokenIssuer
{
    /// <returns>The signed token and how many seconds it stays valid.</returns>
    (string AccessToken, int ExpiresInSeconds) Issue(string subject, string name, string scopes);
}
