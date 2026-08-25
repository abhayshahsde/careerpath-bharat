using CareerPath.Application.Imports;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Import;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class ImportEndpoints
{
    public static void MapImports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/imports")
            .WithTags("Imports")
            .RequireAuthorization();

        // 1. Get pre-signed upload url (Admin only)
        group.MapPost("/upload-url", GetUploadUrl)
            .WithName("GetUploadUrl")
            .WithSummary("Generate signed upload URL for bulk imports (Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // 2. Upload file directly (Simulated storage bucket)
        group.MapPost("/upload/{jobId:guid}", UploadFile)
            .WithName("UploadImportFile")
            .WithSummary("Upload raw CSV/JSON payload for simulated storage bucket")
            .DisableAntiforgery(); // enable raw stream uploads

        // 3. Trigger asynchronous file processing
        group.MapPost("/jobs", ProcessJob)
            .WithName("ProcessImportJob")
            .WithSummary("Trigger staging/validation of uploaded file");

        // 4. List import jobs
        group.MapGet("/jobs", ListJobs)
            .WithName("ListImportJobs")
            .WithSummary("List all import jobs");

        // 5. Get job details
        group.MapGet("/jobs/{id:guid}", GetJobDetail)
            .WithName("GetImportJobDetail")
            .WithSummary("Get import job status, row-validation errors, and milestones");

        // 6. Review import job (Admin only)
        group.MapPost("/jobs/{id:guid}/review", ReviewJob)
            .WithName("ReviewImportJob")
            .WithSummary("Approve or Reject staged import rows (Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> GetUploadUrl(
        UploadUrlRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUploadUrlQuery(
            currentUser.UserId.GetValueOrDefault(), req.ImportType, req.FileName, req.ContentType, req.FileSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadFile(
        Guid jobId, HttpRequest req, IMediator mediator, CancellationToken ct)
    {
        // Get stored path details
        var job = await mediator.Send(new GetImportJobDetailQuery(jobId), ct);
        if (job is null) return Results.NotFound();

        // Mock write to file system scratch folder named after the Job ID
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "uploads");
        Directory.CreateDirectory(baseDir);

        var fileExtension = Path.GetExtension(job.FileName);
        var storedPath = Path.Combine(baseDir, $"{jobId}{fileExtension}");

        await using var fileStream = File.Create(storedPath);
        await req.Body.CopyToAsync(fileStream, ct);

        return Results.Ok(new { success = true, path = storedPath });
    }

    private static IResult ProcessJob(
        CreateImportJobRequest req, IServiceProvider serviceProvider)
    {
        // Enqueue file validation asynchronously with a dedicated service scope
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await scopedMediator.Send(new ProcessImportFileCommand(req.JobId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Async import validation failed: {ex}");
            }
        });
        return Results.Accepted(uri: $"/api/v1/imports/jobs/{req.JobId}", value: new { message = "Import job validation started asynchronously." });
    }

    private static async Task<IResult> ListJobs(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListImportJobsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetJobDetail(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetImportJobDetailQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ReviewJob(
        Guid id, ReviewImportRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var success = await mediator.Send(new ReviewImportJobCommand(id, currentUser.UserId.GetValueOrDefault(), req.IsApproved, req.Notes), ct);
        return success ? Results.Ok(new { success }) : Results.BadRequest(new { error = "Failed to review or apply import job." });
    }
}
