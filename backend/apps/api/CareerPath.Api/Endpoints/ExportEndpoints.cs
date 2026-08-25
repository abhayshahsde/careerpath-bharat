using MediatR;
using Microsoft.AspNetCore.Mvc;
using CareerPath.Application.Exports;
using CareerPath.Application.Abstractions;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Export;

namespace CareerPath.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/exports")
            .WithTags("Exports")
            .RequireAuthorization();

        // 1. Request export job
        group.MapPost("/", RequestExport)
            .WithName("RequestExport")
            .WithSummary("Request asynchronous catalog, roadmap or profile export file generation");

        // 2. List export jobs
        group.MapGet("/jobs", ListJobs)
            .WithName("ListExportJobs")
            .WithSummary("List all export jobs requested by current user");

        // 3. Get export job detail status
        group.MapGet("/jobs/{id:guid}", GetJobDetail)
            .WithName("GetExportJob")
            .WithSummary("Get export job status details and expiring signed download links");

        // 4. Download exported file (signed link verification, does not require auth header since token is in query)
        app.MapGet("/api/v1/exports/download/{id:guid}", DownloadFile)
            .WithTags("Exports")
            .WithName("DownloadExportedFile")
            .WithSummary("Download generated export binary (requires valid URL signature token)")
            .AllowAnonymous(); // Validation performed via token check in query string
    }

    private static async Task<IResult> RequestExport(
        RequestExportRequest req, IMediator mediator, ICurrentUserService currentUser,
        IServiceProvider serviceProvider, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var jobId = await mediator.Send(new RequestExportCommand(userId, req.ExportType, req.Format), ct);

        // Process file generation asynchronously using service scope
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await scopedMediator.Send(new ProcessExportFileCommand(jobId, userId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Async export generation failed: {ex}");
            }
        });

        return Results.Accepted(uri: $"/api/v1/exports/jobs/{jobId}", value: new { jobId, status = "Pending" });
    }

    private static async Task<IResult> ListJobs(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new ListExportJobsQuery(currentUser.UserId.GetValueOrDefault()), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetJobDetail(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetExportJobQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> DownloadFile(
        Guid id, [FromQuery] Guid token, IExportRepository repo, CancellationToken ct)
    {
        var job = await repo.GetJobByTokenAsync(id, token, ct);
        if (job is null)
        {
            return Results.Json(new { error = "Invalid download token or job ID." }, statusCode: 404);
        }

        if (job.Status != "Completed")
        {
            return Results.Json(new { error = "Export file is not fully generated yet." }, statusCode: 400);
        }

        if (DateTimeOffset.UtcNow > job.ExpireAt)
        {
            return Results.Json(new { error = "Download link has expired." }, statusCode: 410);
        }

        // Retrieve internal stored path from DB using repo details directly
        var fullJob = await repo.GetJobAsync(id, ct);
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "exports");
        var filePath = Path.Combine(baseDir, $"export_{id}.{job.Format.ToLowerInvariant()}");

        if (!File.Exists(filePath))
        {
            return Results.Json(new { error = "Exported file not found on disk storage." }, statusCode: 404);
        }

        var contentType = job.Format.ToUpperInvariant() switch
        {
            "PDF"  => "application/pdf",
            "CSV"  => "text/csv",
            "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _      => "application/octet-stream"
        };

        var downloadName = $"{job.ExportType.ToLowerInvariant()}_export_{id}.{job.Format.ToLowerInvariant()}";
        return Results.File(filePath, contentType, downloadName);
    }
}
