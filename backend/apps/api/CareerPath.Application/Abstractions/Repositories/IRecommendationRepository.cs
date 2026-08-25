using CareerPath.Contracts.V1.Recommendation;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IRecommendationRepository
{
    // ── Scoring ───────────────────────────────────────────────────────────────

    /// <summary>Compute and persist scores for all careers for this user.</summary>
    Task ComputeAndSaveScoresAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Return top-N scored careers for a user, sorted by score desc.</summary>
    Task<IReadOnlyList<CareerScoreDto>> GetTopCareersAsync(Guid userId, int take = 10, CancellationToken ct = default);

    /// <summary>Return score for a specific career.</summary>
    Task<CareerScoreDto?> GetScoreAsync(Guid userId, Guid careerId, CancellationToken ct = default);

    // ── Roadmaps ──────────────────────────────────────────────────────────────

    Task<IReadOnlyList<RoadmapSummaryDto>> GetRoadmapsAsync(Guid userId, CancellationToken ct = default);
    Task<RoadmapDetailDto?> GetRoadmapAsync(Guid roadmapId, Guid userId, CancellationToken ct = default);
    Task<Guid> CreateRoadmapAsync(Guid userId, string title, string? description, Guid? careerId, DateOnly? targetDate, CancellationToken ct = default);
    Task DeleteRoadmapAsync(Guid roadmapId, Guid userId, CancellationToken ct = default);

    // ── Milestones ────────────────────────────────────────────────────────────

    Task<int> AddMilestoneAsync(Guid roadmapId, string title, string? description, int sortOrder, CancellationToken ct = default);
    Task CompleteMilestoneAsync(int milestoneId, Guid userId, CancellationToken ct = default);

    // ── Tasks ─────────────────────────────────────────────────────────────────

    Task<int> AddTaskAsync(int milestoneId, string title, string? description, string taskType,
        string? externalUrl, int sortOrder, DateOnly? dueDate,
        int? linkedExamId, int? linkedCourseId, int? linkedSkillId,
        CancellationToken ct = default);

    Task CompleteTaskAsync(int taskId, Guid userId, CancellationToken ct = default);
}
