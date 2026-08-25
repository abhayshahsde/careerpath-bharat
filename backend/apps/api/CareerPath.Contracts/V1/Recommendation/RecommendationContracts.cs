namespace CareerPath.Contracts.V1.Recommendation;

// ── Scored Careers ────────────────────────────────────────────────────────────

public sealed record CareerScoreDto(
    Guid CareerId,
    string CareerTitle,
    string CareerSlug,
    string? CategoryName,
    decimal Score,
    decimal SkillScore,
    decimal EducationScore,
    decimal InterestScore,
    IReadOnlyList<ScoreFactorDto> Factors,
    DateTimeOffset ComputedAt);

public sealed record ScoreFactorDto(
    string FactorKey,
    string Label,
    decimal Weight,
    decimal RawScore,
    decimal WeightedScore,
    string? Reason);

// ── Roadmaps ──────────────────────────────────────────────────────────────────

public sealed record RoadmapSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    Guid? CareerId,
    string? CareerTitle,
    DateTimeOffset CreatedAt,
    int TotalTasks,
    int CompletedTasks,
    int ProgressPercent);

public sealed record RoadmapDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    Guid? CareerId,
    string? CareerTitle,
    DateTimeOffset? TargetDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<MilestoneDto> Milestones);

public sealed record MilestoneDto(
    int Id,
    string Title,
    string? Description,
    int SortOrder,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<TaskDto> Tasks);

public sealed record TaskDto(
    int Id,
    string Title,
    string? Description,
    string TaskType,
    string? ExternalUrl,
    int SortOrder,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    string? DueDate,
    int? LinkedExamId,
    int? LinkedCourseId,
    int? LinkedSkillId);

// ── Requests ──────────────────────────────────────────────────────────────────

public sealed record CreateRoadmapRequest(
    string Title,
    string? Description = null,
    Guid? CareerId = null,
    string? TargetDate = null);

public sealed record AddMilestoneRequest(
    string Title,
    string? Description = null,
    int SortOrder = 0);

public sealed record AddTaskRequest(
    string Title,
    string? Description = null,
    string TaskType = "General",
    string? ExternalUrl = null,
    int SortOrder = 0,
    string? DueDate = null,
    int? LinkedExamId = null,
    int? LinkedCourseId = null,
    int? LinkedSkillId = null);
