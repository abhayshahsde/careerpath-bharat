using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.NotificationAnalytics;

namespace CareerPath.Application.NotificationAnalytics;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record ListNotificationsQuery(Guid UserId, bool UnreadOnly) : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class ListNotificationsHandler : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repo;
    public ListNotificationsHandler(INotificationRepository repo) => _repo = repo;
    public Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery q, CancellationToken ct)
        => _repo.ListNotificationsAsync(q.UserId, q.UnreadOnly, ct);
}

// ── Mark Read Commands ────────────────────────────────────────────────────────

public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest;

public sealed class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _repo;
    public MarkNotificationReadHandler(INotificationRepository repo) => _repo = repo;

    public async Task Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        await _repo.MarkAsReadAsync(cmd.UserId, cmd.NotificationId, ct);
    }
}

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest;

public sealed class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _repo;
    public MarkAllNotificationsReadHandler(INotificationRepository repo) => _repo = repo;

    public async Task Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        await _repo.MarkAllAsReadAsync(cmd.UserId, ct);
    }
}

// ── Log Telemetry Command ────────────────────────────────────────────────────

public sealed record LogTelemetryCommand(Guid? UserId, string EventName, string? PayloadJson) : IRequest;

public sealed class LogTelemetryValidator : AbstractValidator<LogTelemetryCommand>
{
    public LogTelemetryValidator()
    {
        RuleFor(x => x.EventName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PayloadJson).MaximumLength(8000).When(x => x.PayloadJson is not null);
    }
}

public sealed class LogTelemetryHandler : IRequestHandler<LogTelemetryCommand>
{
    private readonly INotificationRepository _repo;
    public LogTelemetryHandler(INotificationRepository repo) => _repo = repo;

    public async Task Handle(LogTelemetryCommand cmd, CancellationToken ct)
    {
        await _repo.LogTelemetryEventAsync(cmd.UserId, cmd.EventName, cmd.PayloadJson, ct);
    }
}
