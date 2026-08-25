using CareerPath.Application.Catalog;
using CareerPath.Contracts.V1.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalog(this IEndpointRouteBuilder app)
    {
        // Categories
        var catGroup = app.MapGroup("/api/v1/categories").WithTags("Catalog – Categories");
        catGroup.MapGet("/", GetCategories).WithName("GetCategories").WithSummary("List all career categories");

        // Skills
        var skillGroup = app.MapGroup("/api/v1/skills").WithTags("Catalog – Skills");
        skillGroup.MapGet("/", GetSkills).WithName("GetSkills").WithSummary("List all skills");

        // Exams
        var examGroup = app.MapGroup("/api/v1/exams").WithTags("Catalog – Exams");
        examGroup.MapGet("/",        GetExams).WithName("GetExams").WithSummary("List entrance exams (paginated)");
        examGroup.MapGet("/{slug}",  GetExamBySlug).WithName("GetExamBySlug").WithSummary("Get exam detail");

        // Courses
        var courseGroup = app.MapGroup("/api/v1/courses").WithTags("Catalog – Courses");
        courseGroup.MapGet("/",       GetCourses).WithName("GetCourses").WithSummary("List courses (paginated)");
        courseGroup.MapGet("/{slug}", GetCourseBySlug).WithName("GetCourseBySlug").WithSummary("Get course detail");

        // Scholarships
        var schGroup = app.MapGroup("/api/v1/scholarships").WithTags("Catalog – Scholarships");
        schGroup.MapGet("/",       GetScholarships).WithName("GetScholarships").WithSummary("List scholarships (paginated)");
        schGroup.MapGet("/{slug}", GetScholarshipBySlug).WithName("GetScholarshipBySlug").WithSummary("Get scholarship detail");

        // Career detail (enriched)
        app.MapGet("/api/v1/careers/{slug}/detail", GetCareerDetail)
            .WithName("GetCareerDetail")
            .WithSummary("Get enriched career detail with skills, exams and courses")
            .WithTags("Careers");
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> GetCategories(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSkills(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSkillsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetExams(
        IMediator mediator, CancellationToken ct,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetExamsQuery(level, search, page, pageSize, locale), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetExamBySlug(
        string slug, IMediator mediator, CancellationToken ct,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetExamBySlugQuery(slug, locale), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetCourses(
        IMediator mediator, CancellationToken ct,
        [FromQuery] string? degreeLevel = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetCoursesQuery(degreeLevel, categoryId, search, page, pageSize, locale), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCourseBySlug(
        string slug, IMediator mediator, CancellationToken ct,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetCourseBySlugQuery(slug, locale), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetScholarships(
        IMediator mediator, CancellationToken ct,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetScholarshipsQuery(level, search, page, pageSize, locale), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetScholarshipBySlug(
        string slug, IMediator mediator, CancellationToken ct,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetScholarshipBySlugQuery(slug, locale), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetCareerDetail(
        string slug, IMediator mediator, CancellationToken ct,
        [FromQuery] string locale = "en")
    {
        var result = await mediator.Send(new GetCareerDetailQuery(slug, locale), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
