using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Recommendation;

namespace CareerPath.Application.Recommendations;

// ── Get Recommendations ───────────────────────────────────────────────────────

public sealed record GetRecommendationsQuery(Guid UserId, int Take = 10) : IRequest<IReadOnlyList<CareerScoreDto>>;

public sealed class GetRecommendationsHandler : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<CareerScoreDto>>
{
    private readonly IRecommendationRepository _repo;
    public GetRecommendationsHandler(IRecommendationRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CareerScoreDto>> Handle(GetRecommendationsQuery q, CancellationToken ct)
    {
        // Compute fresh scores, then return top-N
        await _repo.ComputeAndSaveScoresAsync(q.UserId, ct);
        return await _repo.GetTopCareersAsync(q.UserId, q.Take, ct);
    }
}

public sealed record GetCareerScoreQuery(Guid UserId, Guid CareerId) : IRequest<CareerScoreDto?>;

public sealed class GetCareerScoreHandler : IRequestHandler<GetCareerScoreQuery, CareerScoreDto?>
{
    private readonly IRecommendationRepository _repo;
    public GetCareerScoreHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<CareerScoreDto?> Handle(GetCareerScoreQuery q, CancellationToken ct)
        => _repo.GetScoreAsync(q.UserId, q.CareerId, ct);
}

// ── Roadmaps ──────────────────────────────────────────────────────────────────

public sealed record GetRoadmapsQuery(Guid UserId) : IRequest<IReadOnlyList<RoadmapSummaryDto>>;

public sealed class GetRoadmapsHandler : IRequestHandler<GetRoadmapsQuery, IReadOnlyList<RoadmapSummaryDto>>
{
    private readonly IRecommendationRepository _repo;
    public GetRoadmapsHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<IReadOnlyList<RoadmapSummaryDto>> Handle(GetRoadmapsQuery q, CancellationToken ct)
        => _repo.GetRoadmapsAsync(q.UserId, ct);
}

public sealed record GetRoadmapQuery(Guid RoadmapId, Guid UserId) : IRequest<RoadmapDetailDto?>;

public sealed class GetRoadmapHandler : IRequestHandler<GetRoadmapQuery, RoadmapDetailDto?>
{
    private readonly IRecommendationRepository _repo;
    public GetRoadmapHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<RoadmapDetailDto?> Handle(GetRoadmapQuery q, CancellationToken ct)
        => _repo.GetRoadmapAsync(q.RoadmapId, q.UserId, ct);
}

public sealed record CreateRoadmapCommand(
    Guid UserId,
    string Title,
    string? Description,
    Guid? CareerId,
    DateOnly? TargetDate) : IRequest<Guid>;

public sealed class CreateRoadmapValidator : AbstractValidator<CreateRoadmapCommand>
{
    public CreateRoadmapValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.TargetDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today))
            .When(x => x.TargetDate.HasValue)
            .WithMessage("Target date must be in the future.");
    }
}

public sealed class CreateRoadmapHandler : IRequestHandler<CreateRoadmapCommand, Guid>
{
    private readonly IRecommendationRepository _repo;
    public CreateRoadmapHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<Guid> Handle(CreateRoadmapCommand cmd, CancellationToken ct)
        => _repo.CreateRoadmapAsync(cmd.UserId, cmd.Title, cmd.Description, cmd.CareerId, cmd.TargetDate, ct);
}

public sealed record DeleteRoadmapCommand(Guid RoadmapId, Guid UserId) : IRequest;

public sealed class DeleteRoadmapHandler : IRequestHandler<DeleteRoadmapCommand>
{
    private readonly IRecommendationRepository _repo;
    public DeleteRoadmapHandler(IRecommendationRepository repo) => _repo = repo;
    public Task Handle(DeleteRoadmapCommand cmd, CancellationToken ct)
        => _repo.DeleteRoadmapAsync(cmd.RoadmapId, cmd.UserId, ct);
}

// ── Milestones ────────────────────────────────────────────────────────────────

public sealed record AddMilestoneCommand(
    Guid RoadmapId,
    string Title,
    string? Description,
    int SortOrder) : IRequest<int>;

public sealed class AddMilestoneValidator : AbstractValidator<AddMilestoneCommand>
{
    public AddMilestoneValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddMilestoneHandler : IRequestHandler<AddMilestoneCommand, int>
{
    private readonly IRecommendationRepository _repo;
    public AddMilestoneHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<int> Handle(AddMilestoneCommand cmd, CancellationToken ct)
        => _repo.AddMilestoneAsync(cmd.RoadmapId, cmd.Title, cmd.Description, cmd.SortOrder, ct);
}

public sealed record CompleteMilestoneCommand(int MilestoneId, Guid UserId) : IRequest;

public sealed class CompleteMilestoneHandler : IRequestHandler<CompleteMilestoneCommand>
{
    private readonly IRecommendationRepository _repo;
    public CompleteMilestoneHandler(IRecommendationRepository repo) => _repo = repo;
    public Task Handle(CompleteMilestoneCommand cmd, CancellationToken ct)
        => _repo.CompleteMilestoneAsync(cmd.MilestoneId, cmd.UserId, ct);
}

// ── Tasks ─────────────────────────────────────────────────────────────────────

public sealed record AddTaskCommand(
    int MilestoneId,
    string Title,
    string? Description,
    string TaskType,
    string? ExternalUrl,
    int SortOrder,
    DateOnly? DueDate,
    int? LinkedExamId,
    int? LinkedCourseId,
    int? LinkedSkillId) : IRequest<int>;

public sealed class AddTaskValidator : AbstractValidator<AddTaskCommand>
{
    private static readonly string[] ValidTypes =
        ["General", "StudyMaterial", "ExamPrep", "CourseEnrollment", "SkillPractice"];

    public AddTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.TaskType).Must(t => ValidTypes.Contains(t))
            .WithMessage($"TaskType must be one of: {string.Join(", ", ValidTypes)}");
        RuleFor(x => x.DueDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today))
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future.");
    }
}

public sealed class AddTaskHandler : IRequestHandler<AddTaskCommand, int>
{
    private readonly IRecommendationRepository _repo;
    public AddTaskHandler(IRecommendationRepository repo) => _repo = repo;
    public Task<int> Handle(AddTaskCommand cmd, CancellationToken ct)
        => _repo.AddTaskAsync(cmd.MilestoneId, cmd.Title, cmd.Description, cmd.TaskType,
            cmd.ExternalUrl, cmd.SortOrder, cmd.DueDate,
            cmd.LinkedExamId, cmd.LinkedCourseId, cmd.LinkedSkillId, ct);
}

public sealed record CompleteTaskCommand(int TaskId, Guid UserId) : IRequest;

public sealed class CompleteTaskHandler : IRequestHandler<CompleteTaskCommand>
{
    private readonly IRecommendationRepository _repo;
    public CompleteTaskHandler(IRecommendationRepository repo) => _repo = repo;
    public Task Handle(CompleteTaskCommand cmd, CancellationToken ct)
        => _repo.CompleteTaskAsync(cmd.TaskId, cmd.UserId, ct);
}
