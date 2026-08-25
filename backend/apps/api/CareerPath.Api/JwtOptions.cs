using System.ComponentModel.DataAnnotations;

namespace CareerPath.Api;

/// <summary>
/// Strongly-typed JWT options, validated at startup.
/// Values must come from environment variables or secret manager — never hard-coded.
/// </summary>
public sealed class JwtOptions
{
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}
