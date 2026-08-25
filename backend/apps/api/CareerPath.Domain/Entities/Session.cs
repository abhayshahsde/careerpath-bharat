namespace CareerPath.Domain.Entities;

/// <summary>
/// Refresh token — hashed, belongs to a session family.
/// If a consumed token is replayed, the entire family is revoked (reuse detection).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public Guid FamilyId { get; init; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsUsed   => UsedAt.HasValue;
    public bool IsValid  => !IsExpired && !IsUsed;
}

/// <summary>
/// Session — groups all refresh tokens under one device login.
/// Revoking a session invalidates all tokens in it.
/// </summary>
public sealed class Session
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;
}
