using System.Security.Claims;
using MACHTEN.Application.Contracts.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MACHTEN.Api.Infrastructure.Auth;

/// <summary>
/// Signs access tokens with the same RSA key the JWT Bearer handler validates
/// against. JsonWebTokenHandler is the modern handler — it does not rewrite
/// claim names and is trimming/AOT friendly, unlike JwtSecurityTokenHandler.
/// </summary>
public sealed class TokenIssuer(SigningCredentials credentials, string issuer) : ITokenIssuer
{
    /// <summary>Short by design: there is no refresh flow to lean on.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private static readonly JsonWebTokenHandler Handler = new();

    public (string AccessToken, int ExpiresInSeconds) Issue(string subject, string name, string scopes)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(Lifetime),
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [ClaimNames.Subject] = subject,
                [ClaimNames.Name] = name,
                [ClaimNames.Scope] = scopes
            }
        };

        return (Handler.CreateToken(descriptor), (int)Lifetime.TotalSeconds);
    }
}

/// <summary>
/// The claim names this API issues and reads. Spelled out here rather than
/// pulled from a library so both sides of the exchange use one definition.
/// </summary>
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Scope = "scope";
}
