using CareerPath.Application.Editorial;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Editorial;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class EditorialEndpoints
{
    public static void MapEditorial(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/editorial/articles")
            .WithTags("Editorial")
            .RequireAuthorization();

        // Public read endpoints (no auth required)
        app.MapGet("/api/v1/articles", GetArticles)
            .WithTags("Editorial – Public")
            .WithName("GetPublishedArticles")
            .WithSummary("List published articles (public)");

        app.MapGet("/api/v1/articles/{slug}", GetArticleBySlug)
            .WithTags("Editorial – Public")
            .WithName("GetPublishedArticleBySlug")
            .WithSummary("Get published article by slug (public)");

        // Protected editorial endpoints
        group.MapGet("/", ListAllArticles)
            .WithName("ListAllArticles")
            .WithSummary("List all articles (editors/admins)");

        group.MapGet("/{id:guid}", GetArticleById)
            .WithName("GetArticleById")
            .WithSummary("Get article detail by ID");

        group.MapPost("/", CreateArticle)
            .WithName("CreateArticle")
            .WithSummary("Create new article draft");

        group.MapPost("/{id:guid}/versions", SaveVersion)
            .WithName("SaveArticleVersion")
            .WithSummary("Save new content version");

        group.MapPost("/{id:guid}/submit", SubmitForReview)
            .WithName("SubmitArticleForReview")
            .WithSummary("Submit article for editorial review");

        group.MapPost("/{id:guid}/reviews/{reviewId:int}/decision", RecordReviewDecision)
            .WithName("RecordReviewDecision")
            .WithSummary("Approve, request changes, or reject an article");

        group.MapPost("/{id:guid}/publish", PublishArticle)
            .WithName("PublishArticle")
            .WithSummary("Publish or schedule article");

        group.MapPost("/{id:guid}/archive", ArchiveArticle)
            .WithName("ArchiveArticle")
            .WithSummary("Archive a published article");
    }

    // ── Public endpoints ──────────────────────────────────────────────────────

    private static async Task<IResult> GetArticles(
        IMediator mediator, CancellationToken ct,
        [FromQuery] string? locale = "en",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(
            new ListArticlesQuery("Published", null, locale, null, search, page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetArticleBySlug(
        string slug, IMediator mediator, CancellationToken ct,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetArticleBySlugQuery(slug, locale), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ── Protected editorial endpoints ─────────────────────────────────────────

    private static async Task<IResult> ListAllArticles(
        IMediator mediator, CancellationToken ct,
        [FromQuery] string? status = null,
        [FromQuery] string? articleType = null,
        [FromQuery] string? locale = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(
            new ListArticlesQuery(status, articleType, locale, null, search, page, pageSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetArticleById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetArticleByIdQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateArticle(
        CreateArticleRequest req, IMediator mediator,
        ICurrentUserService currentUser, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateArticleCommand(
            req.Slug, req.ArticleType, req.Locale,
            currentUser.UserId.GetValueOrDefault(), req.LinkedCareerId,
            req.Title, req.Body, req.Summary,
            req.MetaDescription, req.Keywords), ct);
        return Results.Created($"/api/v1/editorial/articles/{id}", new { id });
    }

    private static async Task<IResult> SaveVersion(
        Guid id, SaveVersionRequest req,
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var versionId = await mediator.Send(new SaveVersionCommand(
            id, currentUser.UserId.GetValueOrDefault(), req.Title, req.Body,
            req.Summary, req.MetaDescription, req.Keywords, req.ChangeNote), ct);
        return Results.Ok(new { versionId });
    }

    private static async Task<IResult> SubmitForReview(
        Guid id, SubmitForReviewRequest req,
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new SubmitForReviewCommand(id, currentUser.UserId.GetValueOrDefault(), req.ReviewerId, req.DueBy), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RecordReviewDecision(
        Guid id, int reviewId, ReviewDecisionRequest req,
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new RecordReviewDecisionCommand(
            id, reviewId, currentUser.UserId.GetValueOrDefault(), req.Decision, req.Feedback), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PublishArticle(
        Guid id, PublishArticleRequest req,
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new PublishArticleCommand(id, currentUser.UserId.GetValueOrDefault(), req.ScheduledAt), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ArchiveArticle(
        Guid id, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        await mediator.Send(new ArchiveArticleCommand(id, currentUser.UserId.GetValueOrDefault()), ct);
        return Results.NoContent();
    }
}
