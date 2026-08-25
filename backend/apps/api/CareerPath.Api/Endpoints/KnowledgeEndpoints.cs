using MediatR;
using Microsoft.AspNetCore.Mvc;
using CareerPath.Application.Imports;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Knowledge;

namespace CareerPath.Api.Endpoints;

public static class KnowledgeEndpoints
{
    public static void MapKnowledge(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/knowledge")
            .WithTags("Knowledge Base")
            .RequireAuthorization();

        // 1. Request document upload metadata (Admin only)
        group.MapPost("/", RequestUpload)
            .WithName("RequestDocumentUpload")
            .WithSummary("Register new syllabus/exam document upload job (Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // 2. Upload file payload directly
        group.MapPost("/upload/{id:guid}", UploadFile)
            .WithName("UploadDocumentFile")
            .WithSummary("Upload raw text or PDF payload stream")
            .DisableAntiforgery();

        // 3. Trigger document chunking process
        group.MapPost("/jobs", ProcessJob)
            .WithName("ProcessKnowledgeJob")
            .WithSummary("Trigger staging, extraction and chunk splitting");

        // 4. List all documents
        group.MapGet("/", ListDocuments)
            .WithName("ListDocuments")
            .WithSummary("List all documents in the knowledge base catalog");

        // 5. Get document detail with chunk segments
        group.MapGet("/{id:guid}", GetDocumentDetail)
            .WithName("GetDocumentDetail")
            .WithSummary("Get document details and split chunk segment text");

        // 6. Update single chunk content (Admin only)
        group.MapPut("/chunks/{chunkId:long}", UpdateChunk)
            .WithName("UpdateDocumentChunk")
            .WithSummary("Edit extracted text inside a chunk and set reviewed status (Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // 7. Approve / Reject document (Admin only)
        group.MapPost("/{id:guid}/review", ReviewDocument)
            .WithName("ReviewDocument")
            .WithSummary("Approve or Reject document chunking before final indexing (Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> RequestUpload(
        UploadDocumentRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new UploadDocumentCommand(
            currentUser.UserId.GetValueOrDefault(), req.Title, req.DocType, req.FileName, req.FileSize), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadFile(
        Guid id, HttpRequest req, IMediator mediator, CancellationToken ct)
    {
        var doc = await mediator.Send(new GetDocumentQuery(id), ct);
        if (doc is null) return Results.NotFound();

        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "knowledge");
        Directory.CreateDirectory(baseDir);

        var fileExtension = Path.GetExtension(doc.FilePath);
        var storedPath = Path.Combine(baseDir, $"{id}{fileExtension}");

        await using var fileStream = File.Create(storedPath);
        await req.Body.CopyToAsync(fileStream, ct);

        return Results.Ok(new { success = true, path = storedPath });
    }

    private static IResult ProcessJob(
        [FromQuery] Guid documentId, IServiceProvider serviceProvider)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await scopedMediator.Send(new ProcessDocumentCommand(documentId), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Async document chunking failed: {ex}");
            }
        });

        return Results.Accepted(uri: $"/api/v1/knowledge/{documentId}", value: new { message = "Document chunking started asynchronously." });
    }

    private static async Task<IResult> ListDocuments(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListDocumentsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDocumentDetail(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDocumentQuery(id), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateChunk(
        long chunkId, UpdateChunkRequest req, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new UpdateChunkCommand(chunkId, req.Content, req.IsReviewed), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReviewDocument(
        Guid id, ReviewDocumentRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var success = await mediator.Send(new ReviewDocumentCommand(id, currentUser.UserId.GetValueOrDefault(), req.IsApproved, req.Notes), ct);
        return success ? Results.Ok(new { success }) : Results.BadRequest(new { error = "Failed to submit document review." });
    }
}
