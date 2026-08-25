using CareerPath.Application.Recommendations;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Recommendation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class RecommendationEndpoints
{
    public static void MapRecommendations(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/v1/me")
            .WithTags("Recommendations")
            .RequireAuthorization();

        // Career recommendations (scored)
        me.MapGet("/recommendations", GetRecommendations)
            .WithName("GetCareerRecommendations")
            .WithSummary("Get personalised career recommendations (triggers score computation)");

        me.MapGet("/recommendations/{careerId:guid}", GetCareerScore)
            .WithName("GetCareerScore")
            .WithSummary("Get fit score for a specific career");

        // Roadmaps
        me.MapGet("/roadmaps", GetRoadmaps)
            .WithName("GetRoadmaps")
            .WithSummary("List all roadmaps for current user");

        me.MapGet("/roadmaps/{id:guid}", GetRoadmap)
            .WithName("GetRoadmap")
            .WithSummary("Get roadmap with full milestone and task detail");

        me.MapPost("/roadmaps", CreateRoadmap)
            .WithName("CreateRoadmap")
            .WithSummary("Create a new learning roadmap");

        me.MapDelete("/roadmaps/{id:guid}", DeleteRoadmap)
            .WithName("DeleteRoadmap")
            .WithSummary("Delete a roadmap");

        // Milestones
        me.MapPost("/roadmaps/{id:guid}/milestones", AddMilestone)
            .WithName("AddMilestone")
            .WithSummary("Add a milestone to a roadmap");

        me.MapPost("/roadmaps/{id:guid}/milestones/{milestoneId:int}/complete", CompleteMilestone)
            .WithName("CompleteMilestone")
            .WithSummary("Mark a milestone as completed");

        // Tasks
        me.MapPost("/roadmaps/{id:guid}/milestones/{milestoneId:int}/tasks", AddTask)
            .WithName("AddRoadmapTask")
            .WithSummary("Add a task to a milestone");

        me.MapPost("/roadmaps/{id:guid}/milestones/{milestoneId:int}/tasks/{taskId:int}/complete", CompleteTask)
            .WithName("CompleteTask")
            .WithSummary("Mark a task as completed");
    }

    // ── Scoring ───────────────────────────────────────────────────────────────

    private static async Task<IResult> GetRecommendations(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct,
        [FromQuery] int take = 10)
    {
        var result = await mediator.Send(new GetRecommendationsQuery(currentUser.UserId.GetValueOrDefault(), Math.Clamp(take, 1, 50)), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCareerScore(
        Guid careerId, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCareerScoreQuery(currentUser.UserId.GetValueOrDefault(), careerId), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ── Roadmaps ──────────────────────────────────────────────────────────────

    private static async Task<IResult> GetRoadmaps(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoadmapsQuery(currentUser.UserId.GetValueOrDefault()), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoadmap(
        Guid id, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRoadmapQuery(id, currentUser.UserId.GetValueOrDefault()), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateRoadmap(
        CreateRoadmapRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        DateOnly? targetDate = req.TargetDate is not null && DateOnly.TryParse(req.TargetDate, out var d) ? d : null;
        var id = await mediator.Send(new CreateRoadmapCommand(
            currentUser.UserId.GetValueOrDefault(), req.Title, req.Description, req.CareerId, targetDate), ct);
        return Results.Created($"/api/v1/me/roadmaps/{id}", new { id });
    }

    private static async Task<IResult> DeleteRoadmap(
        Guid id, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new DeleteRoadmapCommand(id, currentUser.UserId.GetValueOrDefault()), ct);
        return Results.NoContent();
    }

    // ── Milestones ────────────────────────────────────────────────────────────

    private static async Task<IResult> AddMilestone(
        Guid id, AddMilestoneRequest req, IMediator mediator, CancellationToken ct)
    {
        var milestoneId = await mediator.Send(new AddMilestoneCommand(id, req.Title, req.Description, req.SortOrder), ct);
        return Results.Created($"/api/v1/me/roadmaps/{id}", new { milestoneId });
    }

    private static async Task<IResult> CompleteMilestone(
        Guid id, int milestoneId, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new CompleteMilestoneCommand(milestoneId, currentUser.UserId.GetValueOrDefault()), ct);
        return Results.NoContent();
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> AddTask(
        Guid id, int milestoneId, AddTaskRequest req, IMediator mediator, CancellationToken ct)
    {
        DateOnly? due = req.DueDate is not null && DateOnly.TryParse(req.DueDate, out var d) ? d : null;
        var taskId = await mediator.Send(new AddTaskCommand(
            milestoneId, req.Title, req.Description, req.TaskType,
            req.ExternalUrl, req.SortOrder, due,
            req.LinkedExamId, req.LinkedCourseId, req.LinkedSkillId), ct);
        return Results.Created($"/api/v1/me/roadmaps/{id}", new { taskId });
    }

    private static async Task<IResult> CompleteTask(
        Guid id, int milestoneId, int taskId,
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new CompleteTaskCommand(taskId, currentUser.UserId.GetValueOrDefault()), ct);
        return Results.NoContent();
    }
}
