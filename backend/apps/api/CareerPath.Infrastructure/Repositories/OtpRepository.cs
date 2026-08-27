using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class OtpRepository : IOtpRepository
{
    private readonly ISqlConnectionFactory _db;
    private static bool _tableEnsured;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public OtpRepository(ISqlConnectionFactory db)
    {
        _db = db;
    }

    private async Task EnsureTableAsync(CancellationToken ct)
    {
        if (_tableEnsured) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_tableEnsured) return;

            await using var conn = await _db.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(
                """
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetOtps' AND schema_id = SCHEMA_ID('identity'))
                BEGIN
                    CREATE TABLE [identity].[PasswordResetOtps] (
                        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
                        Identifier NVARCHAR(255) NOT NULL,
                        OtpCode NVARCHAR(10) NOT NULL,
                        Channel NVARCHAR(50) NOT NULL,
                        ResetToken NVARCHAR(255) NULL,
                        ExpiresAt DATETIMEOFFSET NOT NULL,
                        AttemptCount INT NOT NULL DEFAULT 0,
                        IsUsed BIT NOT NULL DEFAULT 0,
                        CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                    CREATE INDEX IX_PasswordResetOtps_Identifier ON [identity].[PasswordResetOtps](Identifier);
                    CREATE INDEX IX_PasswordResetOtps_ResetToken ON [identity].[PasswordResetOtps](ResetToken);
                END
                """);

            _tableEnsured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task CreateOtpAsync(string identifier, string otpCode, string channel, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        // Invalidate prior unused OTPs for this identifier
        await conn.ExecuteAsync(
            """
            UPDATE [identity].[PasswordResetOtps]
            SET IsUsed = 1
            WHERE Identifier = @Identifier AND IsUsed = 0
            """,
            new { Identifier = identifier.ToLowerInvariant().Trim() });

        // Insert new OTP
        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[PasswordResetOtps] 
                (Id, Identifier, OtpCode, Channel, ExpiresAt, AttemptCount, IsUsed, CreatedAt)
            VALUES 
                (@Id, @Identifier, @OtpCode, @Channel, @ExpiresAt, 0, 0, SYSUTCDATETIME())
            """,
            new
            {
                Id = Guid.NewGuid(),
                Identifier = identifier.ToLowerInvariant().Trim(),
                OtpCode = otpCode,
                Channel = channel,
                ExpiresAt = expiresAt
            });
    }

    public async Task<OtpRecord?> GetActiveOtpAsync(string identifier, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<OtpRecord>(
            """
            SELECT TOP 1 Id, Identifier, OtpCode, Channel, ResetToken, ExpiresAt, AttemptCount, IsUsed, CreatedAt
            FROM [identity].[PasswordResetOtps]
            WHERE Identifier = @Identifier AND IsUsed = 0 AND ExpiresAt > SYSUTCDATETIME()
            ORDER BY CreatedAt DESC
            """,
            new { Identifier = identifier.ToLowerInvariant().Trim() });
    }

    public async Task IncrementAttemptsAsync(Guid otpId, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [identity].[PasswordResetOtps]
            SET AttemptCount = AttemptCount + 1,
                IsUsed = CASE WHEN AttemptCount + 1 >= 5 THEN 1 ELSE IsUsed END
            WHERE Id = @Id
            """,
            new { Id = otpId });
    }

    public async Task<string> MarkOtpVerifiedAndGenerateResetTokenAsync(Guid otpId, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        var resetToken = $"rst_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [identity].[PasswordResetOtps]
            SET ResetToken = @ResetToken,
                IsUsed = 1,
                ExpiresAt = DATEADD(minute, 15, SYSUTCDATETIME())
            WHERE Id = @Id
            """,
            new { Id = otpId, ResetToken = resetToken });

        return resetToken;
    }

    public async Task<OtpRecord?> GetByResetTokenAsync(string resetToken, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        return await conn.QuerySingleOrDefaultAsync<OtpRecord>(
            """
            SELECT TOP 1 Id, Identifier, OtpCode, Channel, ResetToken, ExpiresAt, AttemptCount, IsUsed, CreatedAt
            FROM [identity].[PasswordResetOtps]
            WHERE ResetToken = @ResetToken AND ExpiresAt > SYSUTCDATETIME()
            ORDER BY CreatedAt DESC
            """,
            new { ResetToken = resetToken });
    }

    public async Task InvalidateResetTokenAsync(Guid otpId, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [identity].[PasswordResetOtps]
            SET ResetToken = NULL,
                ExpiresAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new { Id = otpId });
    }
}
