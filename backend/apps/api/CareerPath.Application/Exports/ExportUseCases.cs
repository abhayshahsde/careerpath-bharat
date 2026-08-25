using MediatR;
using FluentValidation;
using System.Text;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Export;

namespace CareerPath.Application.Exports;

// ── Commands ──────────────────────────────────────────────────────────────────

public sealed record RequestExportCommand(
    Guid UserId, string ExportType, string Format) : IRequest<Guid>;

public sealed class RequestExportValidator : AbstractValidator<RequestExportCommand>
{
    private static readonly string[] AllowedTypes = ["Careers", "Roadmaps", "Profile"];
    private static readonly string[] AllowedFormats = ["PDF", "XLSX", "CSV", "DOCX"];

    public RequestExportValidator()
    {
        RuleFor(x => x.ExportType).Must(t => AllowedTypes.Contains(t))
            .WithMessage($"ExportType must be one of: {string.Join(", ", AllowedTypes)}");
        RuleFor(x => x.Format).Must(f => AllowedFormats.Contains(f))
            .WithMessage($"Format must be one of: {string.Join(", ", AllowedFormats)}");
    }
}

public sealed class RequestExportHandler : IRequestHandler<RequestExportCommand, Guid>
{
    private readonly IExportRepository _repo;
    public RequestExportHandler(IExportRepository repo) => _repo = repo;

    public async Task<Guid> Handle(RequestExportCommand cmd, CancellationToken ct)
    {
        // Jobs expire in 2 hours
        return await _repo.CreateJobAsync(cmd.UserId, cmd.ExportType, cmd.Format, TimeSpan.FromHours(2), ct);
    }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public sealed record ListExportJobsQuery(Guid UserId) : IRequest<IReadOnlyList<ExportJobDto>>;

public sealed class ListExportJobsHandler : IRequestHandler<ListExportJobsQuery, IReadOnlyList<ExportJobDto>>
{
    private readonly IExportRepository _repo;
    public ListExportJobsHandler(IExportRepository repo) => _repo = repo;
    public Task<IReadOnlyList<ExportJobDto>> Handle(ListExportJobsQuery q, CancellationToken ct)
        => _repo.ListJobsAsync(q.UserId, ct);
}

public sealed record GetExportJobQuery(Guid Id) : IRequest<ExportJobDto?>;

public sealed class GetExportJobHandler : IRequestHandler<GetExportJobQuery, ExportJobDto?>
{
    private readonly IExportRepository _repo;
    public GetExportJobHandler(IExportRepository repo) => _repo = repo;
    public Task<ExportJobDto?> Handle(GetExportJobQuery q, CancellationToken ct)
        => _repo.GetJobAsync(q.Id, ct);
}

// ── Process Export File Job (Asynchronous Worker) ─────────────────────────────

public sealed record ProcessExportFileCommand(Guid JobId, Guid UserId) : IRequest;

public sealed class ProcessExportFileHandler : IRequestHandler<ProcessExportFileCommand>
{
    private readonly IExportRepository _exportRepo;
    private readonly ICareerRepository _careerRepo;
    private readonly IRecommendationRepository _recommendationRepo;

    public ProcessExportFileHandler(
        IExportRepository exportRepo,
        ICareerRepository careerRepo,
        IRecommendationRepository recommendationRepo)
    {
        _exportRepo = exportRepo;
        _careerRepo = careerRepo;
        _recommendationRepo = recommendationRepo;
    }

    public async Task Handle(ProcessExportFileCommand cmd, CancellationToken ct)
    {
        var job = await _exportRepo.GetJobAsync(cmd.JobId, ct);
        if (job is null || job.Status != "Pending") return;

        await _exportRepo.UpdateJobStatusAsync(cmd.JobId, "Processing", null, null, ct);

        try
        {
            var content = new StringBuilder();
            var fileName = $"export_{cmd.JobId}.{job.Format.ToLowerInvariant()}";

            var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scratch", "exports");
            Directory.CreateDirectory(baseDir);
            var storedPath = Path.Combine(baseDir, fileName);

            if (job.ExportType == "Careers")
            {
                var result = await _careerRepo.GetPublishedAsync("en", null, null, 0, 100, ct);
                
                content.AppendLine("ID,Slug,SalaryRangeLabel,IsFeatured");
                foreach (var c in result.Items)
                {
                    content.AppendLine($"{c.Id},{c.Slug},{c.SalaryRangeLabel},{c.IsFeatured}");
                }
            }
            else if (job.ExportType == "Roadmaps")
            {
                var roadmaps = await _recommendationRepo.GetRoadmapsAsync(cmd.UserId, ct);
                content.AppendLine("RoadmapID,Title,Status,TotalTasks,CompletedTasks,Progress");
                foreach (var r in roadmaps)
                {
                    content.AppendLine($"{r.Id},{r.Title},{r.Status},{r.TotalTasks},{r.CompletedTasks},{r.ProgressPercent}%");
                }
            }
            else // Profile
            {
                content.AppendLine("CareerPath Bharat — Student Profile Export Summary");
                content.AppendLine($"User ID: {cmd.UserId}");
                content.AppendLine($"Export Date: {DateTimeOffset.UtcNow}");
            }

            var bytes = Encoding.UTF8.GetBytes(content.ToString());
            await File.WriteAllBytesAsync(storedPath, bytes, ct);

            await _exportRepo.UpdateJobStatusAsync(cmd.JobId, "Completed", storedPath, null, ct);
        }
        catch (Exception ex)
        {
            await _exportRepo.UpdateJobStatusAsync(cmd.JobId, "Failed", null, ex.Message, ct);
        }
    }
}
