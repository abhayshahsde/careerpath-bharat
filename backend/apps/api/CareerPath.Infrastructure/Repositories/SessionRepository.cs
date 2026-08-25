using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Domain.Entities;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly ISqlConnectionFactory _db;
    public SessionRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<Guid> CreateSessionAsync(
        Guid userId, string? deviceInfo, string? ipAddress, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [identity].[Sessions] (UserId, DeviceInfo, IpAddress, CreatedAt, LastSeenAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @DeviceInfo, @IpAddress, SYSUTCDATETIME(), SYSUTCDATETIME())
            """,
            new { UserId = userId, DeviceInfo = deviceInfo, IpAddress = ipAddress });
    }

    public async Task CreateRefreshTokenAsync(
        Guid sessionId, string tokenHash, Guid familyId, DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[RefreshTokens] (SessionId, TokenHash, FamilyId, ExpiresAt, CreatedAt)
            VALUES (@SessionId, @TokenHash, @FamilyId, @ExpiresAt, SYSUTCDATETIME())
            """,
            new { SessionId = sessionId, TokenHash = tokenHash, FamilyId = familyId, ExpiresAt = expiresAt });
    }

    public async Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<RefreshToken>(
            """
            SELECT Id, SessionId, TokenHash, FamilyId, UsedAt, ExpiresAt, CreatedAt
            FROM [identity].[RefreshTokens]
            WHERE TokenHash = @TokenHash
            """,
            new { TokenHash = tokenHash });
    }

    public async Task MarkRefreshTokenUsedAsync(Guid tokenId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE [identity].[RefreshTokens] SET UsedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = tokenId });
    }

    public async Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken ct = default)
    {
        // Revoke all sessions that own tokens in this family
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [identity].[Sessions]
            SET RevokedAt = SYSUTCDATETIME()
            WHERE Id IN (
                SELECT DISTINCT SessionId FROM [identity].[RefreshTokens]
                WHERE FamilyId = @FamilyId
            ) AND RevokedAt IS NULL
            """,
            new { FamilyId = familyId });
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [identity].[Sessions]
            SET RevokedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId AND RevokedAt IS NULL
            """,
            new { UserId = userId });
    }

    public async Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Session>(
            """
            SELECT Id, UserId, DeviceInfo, IpAddress, CreatedAt, LastSeenAt, RevokedAt
            FROM [identity].[Sessions]
            WHERE Id = @Id
            """,
            new { Id = sessionId });
    }
}
