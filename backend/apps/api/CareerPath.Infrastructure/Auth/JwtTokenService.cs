using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CareerPath.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CareerPath.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(Guid userId, string email, IReadOnlyList<string> roles)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            AccessTokenExpiresAt().UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTimeOffset AccessTokenExpiresAt() =>
        DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

    public (string RawToken, string TokenHash) GenerateRefreshToken()
    {
        var raw   = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash  = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }

    public DateTimeOffset RefreshTokenExpiresAt() =>
        DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenDays);
}

public sealed class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
