using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Knowledge;

namespace CareerPath.Application.Imports; // Namespace Application structure target

public sealed record UploadDocumentCommand(
    Guid UserId, string Title, string DocType, string FileName, long FileSize)
    : IRequest<UploadDocumentResponse>;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedTypes = ["Syllabus", "ExamNotification", "Policy", "CareerGuideline"];

    public UploadDocumentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DocType).Must(t => AllowedTypes.Contains(t))
            .WithMessage($"DocType must be one of: {string.Join(", ", AllowedTypes)}");
        RuleFor(x => x.FileName).NotEmpty().Must(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .txt or .pdf files are supported.");
        RuleFor(x => x.FileSize).GreaterThan(0).LessThanOrEqualTo(20 * 1024 * 1024)
            .WithMessage("File size must be between 1 byte and 20 MB.");
    }
}

public sealed class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    private readonly IKnowledgeRepository _repo;
    public UploadDocumentHandler(IKnowledgeRepository repo) => _repo = repo;

    public async Task<UploadDocumentResponse> Handle(UploadDocumentCommand cmd, CancellationToken ct)
    {
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "knowledge");
        Directory.CreateDirectory(baseDir);

        var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(cmd.FileName)}";
        var filePath = Path.Combine(baseDir, uniqueName);

        var docId = await _repo.CreateDocumentAsync(cmd.UserId, cmd.Title, cmd.DocType, filePath, cmd.FileSize, ct);
        var uploadUrl = $"/api/v1/knowledge/upload/{docId}";

        return new UploadDocumentResponse(docId, uploadUrl, filePath);
    }
}

public sealed record ListDocumentsQuery : IRequest<IReadOnlyList<DocumentSummaryDto>>;

public sealed class ListDocumentsHandler : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>
{
    private readonly IKnowledgeRepository _repo;
    public ListDocumentsHandler(IKnowledgeRepository repo) => _repo = repo;
    public Task<IReadOnlyList<DocumentSummaryDto>> Handle(ListDocumentsQuery q, CancellationToken ct)
        => _repo.ListDocumentsAsync(ct);
}

public sealed record GetDocumentQuery(Guid Id) : IRequest<DocumentDetailDto?>;

public sealed class GetDocumentHandler : IRequestHandler<GetDocumentQuery, DocumentDetailDto?>
{
    private readonly IKnowledgeRepository _repo;
    public GetDocumentHandler(IKnowledgeRepository repo) => _repo = repo;
    public Task<DocumentDetailDto?> Handle(GetDocumentQuery q, CancellationToken ct)
        => _repo.GetDocumentAsync(q.Id, ct);
}

// ── Process Document Chunking (Async background worker) ─────────────────────

public sealed record ProcessDocumentCommand(Guid DocumentId) : IRequest;

public sealed class ProcessDocumentHandler : IRequestHandler<ProcessDocumentCommand>
{
    private readonly IKnowledgeRepository _repo;
    public ProcessDocumentHandler(IKnowledgeRepository repo) => _repo = repo;

    public async Task Handle(ProcessDocumentCommand cmd, CancellationToken ct)
    {
        var doc = await _repo.GetDocumentAsync(cmd.DocumentId, ct);
        if (doc is null || doc.Status != "Pending") return;

        await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Extracting", null, ct);

        try
        {
            var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "knowledge");
            var uniqueFile = Directory.GetFiles(baseDir, $"{cmd.DocumentId}.*").FirstOrDefault();

            if (uniqueFile == null || !File.Exists(uniqueFile))
            {
                await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Failed", "Document file not found on disk.", ct);
                return;
            }

            // In local mock, read text directly. For PDF, read as binary string or mock extract
            var text = "";
            if (uniqueFile.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                text = await File.ReadAllTextAsync(uniqueFile, ct);
            }
            else // PDF mock extraction
            {
                text = $"[PDF EXTRACTED CONTENT] Title: {doc.Title}\nThis is a simulated extraction of syllabus guidelines. " +
                       "Students preparing for Civil Services (UPSC) must check syllabus subjects: Indian Polity, Geography, History, Economics. " +
                       "CSAT requirements require quantitative aptitude and logic practice.";
            }

            // Simple chunking algorithm: split into segments of max 500 characters
            var chunks = new List<(int Index, string Content, int TokenCount)>();
            var index = 1;
            var pos = 0;

            while (pos < text.Length)
            {
                var len = Math.Min(500, text.Length - pos);
                var chunkText = text.Substring(pos, len);
                
                // Approximate token count = word count / 0.75
                var words = chunkText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                var tokens = (int)Math.Ceiling(words / 0.75);

                chunks.Add((index++, chunkText, tokens));
                pos += len;
            }

            await _repo.BulkInsertChunksAsync(cmd.DocumentId, chunks, ct);
            await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Reviewing", null, ct);
        }
        catch (Exception ex)
        {
            await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Failed", $"Chunking failed: {ex.Message}", ct);
        }
    }
}

// ── Edit Chunks Command ───────────────────────────────────────────────────────

public sealed record UpdateChunkCommand(long ChunkId, string Content, bool IsReviewed) : IRequest;

public sealed class UpdateChunkValidator : AbstractValidator<UpdateChunkCommand>
{
    public UpdateChunkValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public sealed class UpdateChunkHandler : IRequestHandler<UpdateChunkCommand>
{
    private readonly IKnowledgeRepository _repo;
    public UpdateChunkHandler(IKnowledgeRepository repo) => _repo = repo;

    public async Task Handle(UpdateChunkCommand cmd, CancellationToken ct)
    {
        await _repo.UpdateChunkAsync(cmd.ChunkId, cmd.Content, cmd.IsReviewed, ct);
    }
}

// ── Review Document Command ───────────────────────────────────────────────────

public sealed record ReviewDocumentCommand(Guid DocumentId, Guid ReviewerId, bool IsApproved, string? Notes) : IRequest<bool>;

public sealed class ReviewDocumentValidator : AbstractValidator<ReviewDocumentCommand>
{
    public ReviewDocumentValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}

public sealed class ReviewDocumentHandler : IRequestHandler<ReviewDocumentCommand, bool>
{
    private readonly IKnowledgeRepository _repo;
    public ReviewDocumentHandler(IKnowledgeRepository repo) => _repo = repo;

    public async Task<bool> Handle(ReviewDocumentCommand cmd, CancellationToken ct)
    {
        await _repo.SubmitReviewAsync(cmd.DocumentId, cmd.ReviewerId, cmd.IsApproved, cmd.Notes, ct);

        if (!cmd.IsApproved)
        {
            await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Failed", $"Rejected by Admin. Notes: {cmd.Notes}", ct);
            return true;
        }

        // Set status to Indexed (Vector Search Adapter will hook this up in Phase 11)
        await _repo.UpdateDocumentStatusAsync(cmd.DocumentId, "Indexed", null, ct);
        return true;
    }
}
