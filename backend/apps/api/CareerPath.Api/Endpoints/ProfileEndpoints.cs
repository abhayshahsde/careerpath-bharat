using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CareerPath.Application.Student;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Student;
using Microsoft.AspNetCore.Builder;

namespace CareerPath.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfile(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me")
            .WithTags("Student Profile")
            .RequireAuthorization();

        group.MapGet("/profile", GetProfile)
            .WithName("GetMyProfile")
            .WithSummary("Get my profile");

        group.MapPut("/profile", UpsertProfile)
            .WithName("UpsertMyProfile")
            .WithSummary("Create or update my profile");

        group.MapGet("/saved-careers", GetSavedCareers)
            .WithName("GetMySavedCareers")
            .WithSummary("List saved careers");

        group.MapPost("/saved-careers/{careerId:guid}", SaveCareer)
            .WithName("SaveCareer")
            .WithSummary("Save a career");

        group.MapDelete("/saved-careers/{careerId:guid}", UnsaveCareer)
            .WithName("UnsaveCareer")
            .WithSummary("Remove a saved career");

        group.MapGet("/saved-courses", GetSavedCourses)
            .WithName("GetMySavedCourses")
            .WithSummary("List saved courses");

        group.MapPost("/saved-courses/{courseId:int}", SaveCourse)
            .WithName("SaveCourse")
            .WithSummary("Save a course");

        group.MapDelete("/saved-courses/{courseId:int}", UnsaveCourse)
            .WithName("UnsaveCourse")
            .WithSummary("Remove a saved course");
    }

    private static async Task<IResult> GetProfile(
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var profile = await mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> UpsertProfile(
        UpsertProfileRequest request,
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var result = await mediator.Send(new UpsertProfileCommand(
            userId,
            request.DisplayName,
            request.CurrentEducationLevel,
            request.StateOfResidence,
            request.PreferredLocale,
            request.SchoolBoard,
            request.StreamOrSubjects,
            request.Interests), cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSavedCareers(
        IMediator mediator,
        ICurrentUserService currentUser,
        [FromQuery] string? locale,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var resolvedLocale = locale ?? "en";
        var result = await mediator.Send(new GetSavedCareersQuery(userId, resolvedLocale), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SaveCareer(
        Guid careerId,
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var saved = await mediator.Send(new SaveCareerCommand(userId, careerId), cancellationToken);
        return saved ? Results.Created($"/api/v1/me/saved-careers/{careerId}", null) : Results.Conflict();
    }

    private static async Task<IResult> UnsaveCareer(
        Guid careerId,
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var removed = await mediator.Send(new UnsaveCareerCommand(userId, careerId), cancellationToken);
        return removed ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetSavedCourses(
        IMediator mediator,
        ICurrentUserService currentUser,
        [FromQuery] string? locale,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var resolvedLocale = locale ?? "en";
        var result = await mediator.Send(new GetSavedCoursesQuery(userId, resolvedLocale), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SaveCourse(
        int courseId,
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var saved = await mediator.Send(new SaveCourseCommand(userId, courseId), cancellationToken);
        return saved ? Results.Created($"/api/v1/me/saved-courses/{courseId}", null) : Results.Conflict();
    }

    private static async Task<IResult> UnsaveCourse(
        int courseId,
        IMediator mediator,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var removed = await mediator.Send(new UnsaveCourseCommand(userId, courseId), cancellationToken);
        return removed ? Results.NoContent() : Results.NotFound();
    }
}
