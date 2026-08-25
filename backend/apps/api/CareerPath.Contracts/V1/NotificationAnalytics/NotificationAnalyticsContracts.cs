namespace CareerPath.Contracts.V1.NotificationAnalytics;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record LogTelemetryRequest(
    string EventName,
    string? PayloadJson);
