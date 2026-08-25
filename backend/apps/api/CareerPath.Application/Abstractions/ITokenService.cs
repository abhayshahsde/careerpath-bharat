using CareerPath.Contracts.V1.Auth;

namespace CareerPath.Application.Abstractions;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// Lives in Application.Abstractions — implemented in Infrastructure.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IReadOnlyList<string> roles);
    DateTimeOffset AccessTokenExpiresAt();
    (string RawToken, string TokenHash) GenerateRefreshToken();
    DateTimeOffset RefreshTokenExpiresAt();
}

/// <summary>
/// Password hashing and verification. BCrypt implementation in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}
