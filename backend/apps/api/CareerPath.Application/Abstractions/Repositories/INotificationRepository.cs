using CareerPath.Contracts.V1.NotificationAnalytics;

namespace CareerPath.Application.Abstractions.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationDto>> ListNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken ct = default);
    Task CreateNotificationAsync(Guid userId, string title, string message, string type, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task LogTelemetryEventAsync(Guid? userId, string eventName, string? payloadJson, CancellationToken ct = default);
}
