using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.NotificationAnalytics;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ISqlConnectionFactory _db;
    public NotificationRepository(ISqlConnectionFactory db) => _db = db;

    private sealed record NotificationDbRow(
        Guid Id, string Title, string Message, string Type, bool IsRead, DateTimeOffset CreatedAt);

    public async Task<IReadOnlyList<NotificationDto>> ListNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var sql = """
            SELECT Id, Title, Message, Type, IsRead, CreatedAt
            FROM [system].[Notifications]
            WHERE UserId = @UserId
            """;

        if (unreadOnly)
        {
            sql += " AND IsRead = 0";
        }

        sql += " ORDER BY CreatedAt DESC";

        var rows = await conn.QueryAsync<NotificationDbRow>(sql, new { UserId = userId });

        return rows.Select(r => new NotificationDto(r.Id, r.Title, r.Message, r.Type, r.IsRead, r.CreatedAt)).ToList();
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string message, string type, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            INSERT INTO [system].[Notifications] (UserId, Title, Message, Type)
            VALUES (@UserId, @Title, @Message, @Type)
            """, new { UserId = userId, Title = title, Message = message, Type = type });
    }

    public async Task MarkAsReadAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [system].[Notifications]
            SET IsRead = 1,
                ReadAt = SYSUTCDATETIME()
            WHERE Id = @Id AND UserId = @UserId
            """, new { Id = id, UserId = userId });
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [system].[Notifications]
            SET IsRead = 1,
                ReadAt = SYSUTCDATETIME()
            WHERE UserId = @UserId AND IsRead = 0
            """, new { UserId = userId });
    }

    public async Task LogTelemetryEventAsync(Guid? userId, string eventName, string? payloadJson, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            INSERT INTO [audit].[TelemetryEvents] (UserId, EventName, Payload)
            VALUES (@UserId, @EventName, @Payload)
            """, new { UserId = userId, EventName = eventName, Payload = payloadJson });
    }
}
