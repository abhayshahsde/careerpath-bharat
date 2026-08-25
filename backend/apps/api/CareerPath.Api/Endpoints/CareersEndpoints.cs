using MediatR;
using CareerPath.Application.Careers;
using CareerPath.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

namespace CareerPath.Api.Endpoints;

public static class CareersEndpoints
{
    public static void MapCareers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/careers")
            .WithTags("Careers");

        group.MapGet("/", GetCareers)
            .WithName("GetCareers")
            .WithSummary("List published careers")
            .WithDescription("Returns a paginated list of published careers, optionally filtered by category, search term and locale.");

        group.MapGet("/{slug}", GetCareerBySlug)
            .WithName("GetCareerBySlug")
            .WithSummary("Get career by slug")
            .WithDescription("Returns the full detail of a single published career by its slug.");
    }

    private static async Task<IResult> GetCareers(
        IMediator mediator,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        [FromQuery] string? locale,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var resolvedLocale = !string.IsNullOrWhiteSpace(locale) ? (locale.StartsWith("hi") ? "hi" : "en") : ParseLocale(acceptLanguage);
        var result = await mediator.Send(
            new GetCareersQuery(resolvedLocale, category, search, page, pageSize),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCareerBySlug(
        string slug,
        IMediator mediator,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        [FromQuery] string? locale,
        CancellationToken cancellationToken = default)
    {
        var resolvedLocale = !string.IsNullOrWhiteSpace(locale) ? (locale.StartsWith("hi") ? "hi" : "en") : ParseLocale(acceptLanguage);
        var result = await mediator.Send(new GetCareerBySlugQuery(slug, resolvedLocale), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static string ParseLocale(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage)) return "en";
        var primary = acceptLanguage.Split(',')[0].Split(';')[0].Trim().ToLowerInvariant();
        return primary.StartsWith("hi") ? "hi" : "en";
    }
}
