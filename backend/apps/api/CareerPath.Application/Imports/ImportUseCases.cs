using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Import;

namespace CareerPath.Application.Imports;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetUploadUrlQuery(
    Guid UserId, string ImportType, string FileName, string ContentType, long FileSize)
    : IRequest<UploadUrlResponse>;

public sealed class GetUploadUrlValidator : AbstractValidator<GetUploadUrlQuery>
{
    private static readonly string[] AllowedTypes = ["Careers", "Exams", "Courses", "Scholarships"];

    public GetUploadUrlValidator()
    {
        RuleFor(x => x.ImportType).Must(t => AllowedTypes.Contains(t))
            .WithMessage($"ImportType must be one of: {string.Join(", ", AllowedTypes)}");
        RuleFor(x => x.FileName).NotEmpty().Must(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .json or .csv files are supported.");
        RuleFor(x => x.FileSize).GreaterThan(0).LessThanOrEqualTo(10 * 1024 * 1024)
            .WithMessage("File size must be between 1 byte and 10 MB.");
    }
}

public sealed class GetUploadUrlHandler : IRequestHandler<GetUploadUrlQuery, UploadUrlResponse>
{
    private readonly IImportRepository _repo;
    public GetUploadUrlHandler(IImportRepository repo) => _repo = repo;

    public async Task<UploadUrlResponse> Handle(GetUploadUrlQuery q, CancellationToken ct)
    {
        // For local development, store imports in a scratch/uploads directory
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "uploads");
        Directory.CreateDirectory(baseDir);

        var fileExtension = Path.GetExtension(q.FileName);
        var uniqueName = $"{Guid.NewGuid()}{fileExtension}";
        var storedPath = Path.Combine(baseDir, uniqueName);

        // Pre-create the job
        var jobId = await _repo.CreateJobAsync(q.UserId, q.ImportType, q.FileName, storedPath, q.ContentType, q.FileSize, ct);

        // Pre-signed URL mock (direct API upload route)
        var uploadUrl = $"/api/v1/imports/upload/{jobId}";

        return new UploadUrlResponse(jobId, uploadUrl, storedPath);
    }
}

public sealed record ListImportJobsQuery : IRequest<IReadOnlyList<ImportJobSummaryDto>>;

public sealed class ListImportJobsHandler : IRequestHandler<ListImportJobsQuery, IReadOnlyList<ImportJobSummaryDto>>
{
    private readonly IImportRepository _repo;
    public ListImportJobsHandler(IImportRepository repo) => _repo = repo;
    public Task<IReadOnlyList<ImportJobSummaryDto>> Handle(ListImportJobsQuery q, CancellationToken ct)
        => _repo.ListJobsAsync(ct);
}

public sealed record GetImportJobDetailQuery(Guid JobId) : IRequest<ImportJobDetailDto?>;

public sealed class GetImportJobDetailHandler : IRequestHandler<GetImportJobDetailQuery, ImportJobDetailDto?>
{
    private readonly IImportRepository _repo;
    public GetImportJobDetailHandler(IImportRepository repo) => _repo = repo;
    public Task<ImportJobDetailDto?> Handle(GetImportJobDetailQuery q, CancellationToken ct)
        => _repo.GetJobAsync(q.JobId, ct);
}

// ── Process File Command ──────────────────────────────────────────────────────

public sealed record ProcessImportFileCommand(Guid JobId) : IRequest;

public sealed class ProcessImportFileHandler : IRequestHandler<ProcessImportFileCommand>
{
    private readonly IImportRepository _repo;
    private readonly ICatalogRepository _catalogRepo;

    public ProcessImportFileHandler(IImportRepository repo, ICatalogRepository catalogRepo)
    {
        _repo = repo;
        _catalogRepo = catalogRepo;
    }

    public async Task Handle(ProcessImportFileCommand cmd, CancellationToken ct)
    {
        var job = await _repo.GetJobAsync(cmd.JobId, ct);
        if (job is null || job.Status != "Created") return;

        await _repo.UpdateJobStatusAsync(cmd.JobId, "Validating", ct: ct);

        // Load existing categories to validate relations
        var categories = (await _catalogRepo.GetCategoriesAsync(ct)).Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stagingRows = new List<(int Index, string Data, string Status, string? Error)>();
        int totalRows = 0;
        int validRows = 0;
        var errorMessages = new List<string>();

        try
        {
            var filePath = job.FileName; // For local mock, retrieve from GetJob storedPath
            // Find the stored path from detail DTO or re-read
            var fullJob = await _repo.GetJobAsync(cmd.JobId, ct);
            var fileLoc = fullJob?.FileName; // Wait, fullJob has FileSize etc. StoredPath is stored in the database.
            // Let's use the local file path:
            var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "uploads");
            var uniqueFile = Directory.GetFiles(baseDir, $"{cmd.JobId}.*").FirstOrDefault();

            if (uniqueFile == null || !File.Exists(uniqueFile))
            {
                await _repo.UpdateJobStatusAsync(cmd.JobId, "Failed", errorSummary: "Upload file not found on disk.", ct: ct);
                return;
            }

            var textContent = await File.ReadAllTextAsync(uniqueFile, ct);
            var items = new List<JsonElement>();

            if (uniqueFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(textContent);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    items = doc.RootElement.EnumerateArray().Select(el => el.Clone()).ToList();
                }
                else
                {
                    items.Add(doc.RootElement.Clone());
                }
            }
            else
            {
                // Simple CSV parser for standard layout
                var lines = textContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    var headers = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToList();
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var values = lines[i].Split(',').Select(v => v.Trim()).ToList();
                        var dict = new Dictionary<string, object>();
                        for (int j = 0; j < Math.Min(headers.Count, values.Count); j++)
                        {
                            dict[headers[j]] = values[j];
                        }
                        var jsonString = JsonSerializer.Serialize(dict);
                        using var d = JsonDocument.Parse(jsonString);
                        items.Add(d.RootElement.Clone());
                    }
                }
            }

            totalRows = items.Count;

            var slugRegex = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");
            var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < items.Count; i++)
            {
                var el = items[i];
                var rowData = el.GetRawText();
                var errors = new List<string>();

                // Check slug
                if (!el.TryGetProperty("slug", out var slugProp) || string.IsNullOrWhiteSpace(slugProp.GetString()))
                {
                    errors.Add("Missing 'slug' property.");
                }
                else
                {
                    var slug = slugProp.GetString()!;
                    if (!slugRegex.IsMatch(slug))
                    {
                        errors.Add($"Invalid slug format: '{slug}'");
                    }
                    if (seenSlugs.Contains(slug))
                    {
                        errors.Add($"Duplicate slug in import: '{slug}'");
                    }
                    else
                    {
                        seenSlugs.Add(slug);
                    }
                }

                // Import type validations
                if (job.ImportType == "Careers")
                {
                    if (!el.TryGetProperty("title", out var titleProp) || string.IsNullOrWhiteSpace(titleProp.GetString()))
                    {
                        errors.Add("Missing 'title' property.");
                    }
                    if (el.TryGetProperty("categoryId", out var catProp) && !string.IsNullOrWhiteSpace(catProp.GetString()))
                    {
                        var cId = catProp.GetString()!;
                        if (!categories.Contains(cId))
                        {
                            errors.Add($"Category '{cId}' does not exist in catalog.");
                        }
                    }
                }
                else if (job.ImportType == "Exams")
                {
                    if (!el.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
                    {
                        errors.Add("Missing 'name' property.");
                    }
                }
                else if (job.ImportType == "Courses")
                {
                    if (!el.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
                    {
                        errors.Add("Missing 'name' property.");
                    }
                    if (!el.TryGetProperty("degreeLevel", out var levelProp) || string.IsNullOrWhiteSpace(levelProp.GetString()))
                    {
                        errors.Add("Missing 'degreeLevel' property.");
                    }
                }
                else if (job.ImportType == "Scholarships")
                {
                    if (!el.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
                    {
                        errors.Add("Missing 'name' property.");
                    }
                    if (!el.TryGetProperty("providerName", out var providerProp) || string.IsNullOrWhiteSpace(providerProp.GetString()))
                    {
                        errors.Add("Missing 'providerName' property.");
                    }
                }

                var rowStatus = errors.Count == 0 ? "Valid" : "Invalid";
                if (rowStatus == "Valid") validRows++;

                var errorMsg = errors.Count > 0 ? string.Join(" | ", errors) : null;
                if (errorMsg is not null)
                {
                    errorMessages.Add($"Row {i + 1}: {errorMsg}");
                }

                stagingRows.Add((i + 1, rowData, rowStatus, errorMsg));
            }

            await _repo.BulkInsertStagingRowsAsync(cmd.JobId, stagingRows, ct);

            var nextStatus = validRows == totalRows ? "Staged" : "FailedValidation";
            var summary = errorMessages.Count > 0 ? JsonSerializer.Serialize(errorMessages.Take(20)) : null;

            await _repo.UpdateJobStatusAsync(cmd.JobId, nextStatus, totalRows, validRows, summary, ct);
        }
        catch (Exception ex)
        {
            await _repo.UpdateJobStatusAsync(cmd.JobId, "Failed", errorSummary: $"Processing exception: {ex.Message}", ct: ct);
        }
    }
}

// ── Review Command ────────────────────────────────────────────────────────────

public sealed record ReviewImportJobCommand(
    Guid JobId, Guid ReviewerId, bool IsApproved, string? Notes) : IRequest<bool>;

public sealed class ReviewImportJobValidator : AbstractValidator<ReviewImportJobCommand>
{
    public ReviewImportJobValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}

public sealed class ReviewImportJobHandler : IRequestHandler<ReviewImportJobCommand, bool>
{
    private readonly IImportRepository _repo;
    public ReviewImportJobHandler(IImportRepository repo) => _repo = repo;

    public async Task<bool> Handle(ReviewImportJobCommand cmd, CancellationToken ct)
    {
        // First record the review decision
        await _repo.SubmitReviewAsync(cmd.JobId, cmd.ReviewerId, cmd.IsApproved, cmd.Notes, ct);

        if (!cmd.IsApproved)
        {
            // Update job to Failed
            await _repo.UpdateJobStatusAsync(cmd.JobId, "Failed", errorSummary: $"Rejected by reviewer. Notes: {cmd.Notes}", ct: ct);
            return true;
        }

        // If approved, trigger transaction to insert staged rows into final catalog
        await _repo.UpdateJobStatusAsync(cmd.JobId, "Importing", ct: ct);
        var success = await _repo.ApplyImportJobAsync(cmd.JobId, cmd.ReviewerId, ct);

        if (!success)
        {
            await _repo.UpdateJobStatusAsync(cmd.JobId, "Failed", errorSummary: "Merging staging rows into catalog failed.", ct: ct);
        }

        return success;
    }
}
