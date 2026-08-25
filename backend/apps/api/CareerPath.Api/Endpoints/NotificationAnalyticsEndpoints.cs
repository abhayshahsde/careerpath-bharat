using MediatR;
using Microsoft.AspNetCore.Mvc;
using CareerPath.Application.NotificationAnalytics;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.NotificationAnalytics;

namespace CareerPath.Api.Endpoints;

public static class NotificationAnalyticsEndpoints
{
    public static void MapNotificationAnalytics(this IEndpointRouteBuilder app)
    {
        // Notifications routing
        var notifGroup = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notifGroup.MapGet("/", ListNotifications)
            .WithName("ListNotifications")
            .WithSummary("List all system alerts and updates for the logged in student");

        notifGroup.MapPost("/{id:guid}/read", MarkRead)
            .WithName("MarkNotificationRead")
            .WithSummary("Mark a single notification alert as read");

        notifGroup.MapPost("/read-all", MarkAllRead)
            .WithName("MarkAllNotificationsRead")
            .WithSummary("Mark all user notifications as read");

        // Analytics routing
        var analyticsGroup = app.MapGroup("/api/v1/analytics")
            .WithTags("Analytics & Telemetry");

        analyticsGroup.MapPost("/events", LogEvent)
            .WithName("LogTelemetryEvent")
            .WithSummary("Post click-stream or roadmap analytics events")
            .AllowAnonymous(); // Enables logging from landing page before login
    }

    private static async Task<IResult> ListNotifications(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct, [FromQuery] bool unreadOnly = false)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var result = await mediator.Send(new ListNotificationsQuery(userId, unreadOnly), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> MarkRead(
        Guid id, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        await mediator.Send(new MarkNotificationReadCommand(userId, id), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllRead(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        await mediator.Send(new MarkAllNotificationsReadCommand(userId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> LogEvent(
        LogTelemetryRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        await mediator.Send(new LogTelemetryCommand(userId, req.EventName, req.PayloadJson), ct);
        return Results.Accepted();
    }
}
