using Dapper;
using System.Text.Json;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Recommendation;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class RecommendationRepository : IRecommendationRepository
{
    private readonly ISqlConnectionFactory _db;
    public RecommendationRepository(ISqlConnectionFactory db) => _db = db;

    // ── Private DB row types (avoids dynamic lambda issues) ───────────────────

    private sealed record ScoreRow(
        Guid CareerId, string CareerTitle, string CareerSlug, string? CategoryName,
        decimal Score, decimal SkillScore, decimal EducationScore, decimal InterestScore,
        string? Explanation, DateTimeOffset ComputedAt);

    private sealed record RoadmapRow(
        Guid Id, string Title, string? Description, string Status, Guid? CareerId,
        string? CareerTitle, DateTimeOffset CreatedAt, int TotalTasks, int CompletedTasks);

    private sealed record RoadmapDetailRow(
        Guid Id, string Title, string? Description, string Status, Guid? CareerId,
        string? CareerTitle, DateTime? TargetDate, DateTimeOffset? CompletedAt, DateTimeOffset CreatedAt);

    private sealed record MilestoneRow(
        int Id, Guid RoadmapId, string Title, string? Description,
        byte SortOrder, bool IsCompleted, DateTimeOffset? CompletedAt);

    private sealed record TaskRow(
        int Id, int MilestoneId, string Title, string? Description, string TaskType,
        string? ExternalUrl, byte SortOrder, bool IsCompleted, DateTimeOffset? CompletedAt,
        DateTime? DueDate, int? LinkedExamId, int? LinkedCourseId, int? LinkedSkillId);

    // ── Scoring ───────────────────────────────────────────────────────────────

    public async Task ComputeAndSaveScoresAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        // Load scoring weights
        var configRows = await conn.QueryAsync<(string FactorKey, decimal Weight)>(
            "SELECT FactorKey, Weight FROM [recommendation].[ScoringConfig] WHERE IsActive = 1");
        var weights = configRows.ToDictionary(r => r.FactorKey, r => r.Weight);

        var skillWeight    = weights.GetValueOrDefault("skill_match",     50m);
        var educWeight     = weights.GetValueOrDefault("education_match", 30m);
        var interestWeight = weights.GetValueOrDefault("interest_match",  20m);

        // User profile — education level, stream/subjects
        var userProfileRow = await conn.QueryFirstOrDefaultAsync<(string? CurrentEducationLevel, string? StreamOrSubjects)>(
            "SELECT CurrentEducationLevel, StreamOrSubjects FROM [student].[Profiles] WHERE UserId = @UserId",
            new { UserId = userId });

        var userEducLevel = userProfileRow.CurrentEducationLevel ?? "";
        var userStream = userProfileRow.StreamOrSubjects ?? "";

        // User career interests
        var interests = (await conn.QueryAsync<string>(
            "SELECT CategoryId FROM [student].[CareerInterests] WHERE UserId = @UserId",
            new { UserId = userId })).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Skills mentioned in onboarding answers
        var userSkillNames = (await conn.QueryAsync<string>(
            "SELECT Answer FROM [student].[OnboardingAnswers] WHERE UserId = @UserId AND QuestionKey LIKE 'skill_%'",
            new { UserId = userId })).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // All published careers with category
        var careers = (await conn.QueryAsync<(Guid Id, string Slug, string? Title, string? CategoryId, int? MinEducationYears)>(
            """
            SELECT c.Id, c.Slug,
                   ct.Title,
                   c.CategoryId,
                   c.MinEducationYears
            FROM [catalog].[Careers] c
            LEFT JOIN [catalog].[CareerTranslations] ct ON ct.CareerId = c.Id AND ct.Locale = 'en'
            WHERE c.Status = 'Published'
            """)).ToList();

        // Career → skills
        var careerSkillRows = (await conn.QueryAsync<(Guid CareerId, string Name)>(
            "SELECT cs.CareerId, s.Name FROM [catalog].[CareerSkills] cs JOIN [catalog].[Skills] s ON s.Id = cs.SkillId"))
            .ToList();

        var careerSkills = careerSkillRows
            .GroupBy(r => r.CareerId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var educLevelMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["10th"] = 0, ["Class 10"] = 0,
            ["12th"] = 1, ["Class 12"] = 1,
            ["diploma"] = 2,
            ["undergraduate"] = 3, ["graduate"] = 3,
            ["postgraduate"] = 4, ["phd"] = 5, ["doctoral"] = 5
        };
        var userEducYears = educLevelMap.TryGetValue(userEducLevel, out var ey) ? ey * 3 : 0;

        // Stream compatibility mapping
        var categoryStreamMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["engineering"] = new(StringComparer.OrdinalIgnoreCase) { "PCM", "Science" },
            ["medicine"] = new(StringComparer.OrdinalIgnoreCase) { "PCB", "Science" },
            ["science"] = new(StringComparer.OrdinalIgnoreCase) { "PCM", "PCB", "Science" },
            ["law"] = new(StringComparer.OrdinalIgnoreCase) { "PCM", "PCB", "Commerce", "Arts", "Science", "General" },
            ["business"] = new(StringComparer.OrdinalIgnoreCase) { "Commerce", "PCM", "PCB", "Arts", "Science", "General" },
            ["arts"] = new(StringComparer.OrdinalIgnoreCase) { "Arts", "General" },
            ["education"] = new(StringComparer.OrdinalIgnoreCase) { "Arts", "Commerce", "PCM", "PCB", "Science", "General" },
            ["government"] = new(StringComparer.OrdinalIgnoreCase) { "Arts", "Commerce", "PCM", "PCB", "Science", "General" },
            ["media"] = new(StringComparer.OrdinalIgnoreCase) { "Arts", "Commerce", "PCM", "PCB", "Science", "General" },
            ["sports"] = new(StringComparer.OrdinalIgnoreCase) { "Arts", "Commerce", "PCM", "PCB", "Science", "General" }
        };

        using var tx = conn.BeginTransaction();

        foreach (var career in careers)
        {
            // 1. Skill Match Score (default to 65% if no skills explicitly selected yet)
            var reqSkills = careerSkills.GetValueOrDefault(career.Id) ?? new HashSet<string>();
            var skillScore = 65m;
            if (reqSkills.Count > 0 && userSkillNames.Count > 0)
            {
                skillScore = (decimal)reqSkills.Count(s => userSkillNames.Contains(s)) / reqSkills.Count * 100;
            }

            // 2. Education Eligibility (don't penalize 10th graders/lower)
            var minYears = career.MinEducationYears ?? 0;
            var educScore = minYears == 0 ? 100m
                : Math.Min(100m, (decimal)userEducYears / minYears * 100);
            if (userEducYears <= 3) // Below college: grant a healthy baseline score
            {
                educScore = Math.Max(educScore, 85m);
            }

            // 3. Interest Score (default to 60% if no interests chosen at all yet)
            var interestScore = 60m;
            if (interests.Count > 0)
            {
                interestScore = career.CategoryId is not null && interests.Contains(career.CategoryId) ? 100m : 25m;
            }

            // 4. Stream Compatibility Multiplier (Only evaluated for Class 11/12/above)
            decimal streamMultiplier = 1.0m;
            if (userEducYears >= 3 && !string.IsNullOrEmpty(userStream) && 
                !userStream.Equals("General", StringComparison.OrdinalIgnoreCase) && 
                !userStream.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                if (categoryStreamMap.TryGetValue(career.CategoryId ?? "", out var compStreams))
                {
                    // If stream is compatible -> 100%, if not -> 40% (still possible, e.g. PCB student doing programming)
                    streamMultiplier = compStreams.Contains(userStream) ? 1.0m : 0.4m;
                }
            }

            var totalScore =
                (skillScore * skillWeight / 100 +
                 educScore * educWeight / 100 +
                 interestScore * interestWeight / 100) * streamMultiplier;

            // Ensure fit score is bounded between 10% and 100% to make UI friendly
            totalScore = Math.Clamp(Math.Round(totalScore, 1), 15.0m, 100.0m);

            var explanationText = $"Base matching factors computed. Stream Compatibility: {streamMultiplier * 100}%.";
            var factors = new[]
            {
                new { FactorKey = "skill_match",     Label = "Skill Match",     Weight = skillWeight,    RawScore = skillScore,    WeightedScore = skillScore    * skillWeight    / 100, Reason = $"{reqSkills.Count(s => userSkillNames.Contains(s))}/{reqSkills.Count} skills matched" },
                new { FactorKey = "education_match", Label = "Education Match", Weight = educWeight,     RawScore = educScore,     WeightedScore = educScore     * educWeight     / 100, Reason = $"User ~{userEducYears}yr, career needs ~{minYears}yr" },
                new { FactorKey = "interest_match",  Label = "Interest Match",  Weight = interestWeight, RawScore = interestScore, WeightedScore = interestScore * interestWeight / 100, Reason = interestScore > 50 ? "Category matches your interests" : "Category not in your interests" }
            };

            await conn.ExecuteAsync(
                """
                MERGE [recommendation].[CareerScores] AS target
                USING (SELECT @UserId AS UserId, @CareerId AS CareerId) AS source
                ON target.UserId = source.UserId AND target.CareerId = source.CareerId
                WHEN MATCHED THEN
                    UPDATE SET Score = @Score, SkillScore = @SkillScore, EducationScore = @EducScore,
                               InterestScore = @InterestScore, Explanation = @Explanation, ComputedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (UserId, CareerId, Score, SkillScore, EducationScore, InterestScore, Explanation)
                    VALUES (@UserId, @CareerId, @Score, @SkillScore, @EducScore, @InterestScore, @Explanation);
                """,
                new
                {
                    UserId = userId, CareerId = career.Id,
                    Score = Math.Round(totalScore, 2),
                    SkillScore = Math.Round(skillScore, 2),
                    EducScore = Math.Round(educScore, 2),
                    InterestScore = Math.Round(interestScore, 2),
                    Explanation = JsonSerializer.Serialize(factors)
                }, tx);
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<CareerScoreDto>> GetTopCareersAsync(Guid userId, int take = 10, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<ScoreRow>(
            """
            SELECT TOP (@Take)
                cs.CareerId, COALESCE(ct.Title,'') AS CareerTitle, c.Slug AS CareerSlug,
                cat.Name AS CategoryName,
                cs.Score, cs.SkillScore, cs.EducationScore, cs.InterestScore,
                cs.Explanation, cs.ComputedAt
            FROM [recommendation].[CareerScores] cs
            JOIN [catalog].[Careers] c ON c.Id = cs.CareerId
            LEFT JOIN [catalog].[CareerTranslations] ct ON ct.CareerId = c.Id AND ct.Locale = 'en'
            LEFT JOIN [catalog].[Categories] cat ON cat.Id = c.CategoryId
            WHERE cs.UserId = @UserId
            ORDER BY cs.Score DESC
            """,
            new { UserId = userId, Take = take });

        return rows.Select(MapToDto).ToList();
    }

    public async Task<CareerScoreDto?> GetScoreAsync(Guid userId, Guid careerId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<ScoreRow>(
            """
            SELECT cs.CareerId, COALESCE(ct.Title,'') AS CareerTitle, c.Slug AS CareerSlug,
                   cat.Name AS CategoryName,
                   cs.Score, cs.SkillScore, cs.EducationScore, cs.InterestScore,
                   cs.Explanation, cs.ComputedAt
            FROM [recommendation].[CareerScores] cs
            JOIN [catalog].[Careers] c ON c.Id = cs.CareerId
            LEFT JOIN [catalog].[CareerTranslations] ct ON ct.CareerId = c.Id AND ct.Locale = 'en'
            LEFT JOIN [catalog].[Categories] cat ON cat.Id = c.CategoryId
            WHERE cs.UserId = @UserId AND cs.CareerId = @CareerId
            """,
            new { UserId = userId, CareerId = careerId });

        return row is null ? null : MapToDto(row);
    }

    private static CareerScoreDto MapToDto(ScoreRow row)
    {
        var factors = new List<ScoreFactorDto>();
        try
        {
            var parsed = JsonSerializer.Deserialize<List<JsonElement>>(row.Explanation ?? "[]");
            if (parsed is not null)
            {
                factors = parsed.Select(f => new ScoreFactorDto(
                    f.GetProperty("FactorKey").GetString()!,
                    f.GetProperty("Label").GetString()!,
                    f.GetProperty("Weight").GetDecimal(),
                    f.GetProperty("RawScore").GetDecimal(),
                    f.GetProperty("WeightedScore").GetDecimal(),
                    f.TryGetProperty("Reason", out var r) ? r.GetString() : null
                )).ToList();
            }
        }
        catch { /* return empty on parse error */ }

        return new CareerScoreDto(
            row.CareerId, row.CareerTitle, row.CareerSlug, row.CategoryName,
            row.Score, row.SkillScore, row.EducationScore, row.InterestScore,
            factors, row.ComputedAt);
    }

    // ── Roadmaps ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RoadmapSummaryDto>> GetRoadmapsAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<RoadmapRow>(
            """
            SELECT r.Id, r.Title, r.Description, r.Status, r.CareerId,
                   ct.Title AS CareerTitle, r.CreatedAt,
                   (SELECT COUNT(*) FROM [recommendation].[Milestones] m
                    JOIN [recommendation].[Tasks] t ON t.MilestoneId = m.Id
                    WHERE m.RoadmapId = r.Id) AS TotalTasks,
                   (SELECT COUNT(*) FROM [recommendation].[Milestones] m
                    JOIN [recommendation].[Tasks] t ON t.MilestoneId = m.Id
                    WHERE m.RoadmapId = r.Id AND t.IsCompleted = 1) AS CompletedTasks
            FROM [recommendation].[Roadmaps] r
            LEFT JOIN [catalog].[Careers] c ON c.Id = r.CareerId
            LEFT JOIN [catalog].[CareerTranslations] ct ON ct.CareerId = c.Id AND ct.Locale = 'en'
            WHERE r.UserId = @UserId
            ORDER BY r.UpdatedAt DESC
            """,
            new { UserId = userId });

        return rows.Select(r =>
        {
            var pct = r.TotalTasks > 0 ? (int)Math.Round((double)r.CompletedTasks / r.TotalTasks * 100) : 0;
            return new RoadmapSummaryDto(r.Id, r.Title, r.Description, r.Status, r.CareerId,
                r.CareerTitle, r.CreatedAt, r.TotalTasks, r.CompletedTasks, pct);
        }).ToList();
    }

    public async Task<RoadmapDetailDto?> GetRoadmapAsync(Guid roadmapId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var roadmap = await conn.QuerySingleOrDefaultAsync<RoadmapDetailRow>(
            """
            SELECT r.Id, r.Title, r.Description, r.Status, r.CareerId,
                   ct.Title AS CareerTitle, r.TargetDate, r.CompletedAt, r.CreatedAt
            FROM [recommendation].[Roadmaps] r
            LEFT JOIN [catalog].[Careers] c ON c.Id = r.CareerId
            LEFT JOIN [catalog].[CareerTranslations] ct ON ct.CareerId = c.Id AND ct.Locale = 'en'
            WHERE r.Id = @Id
            """,
            new { Id = roadmapId, UserId = userId });

        if (roadmap is null) return null;

        var milestones = (await conn.QueryAsync<MilestoneRow>(
            "SELECT Id, RoadmapId, Title, Description, SortOrder, IsCompleted, CompletedAt FROM [recommendation].[Milestones] WHERE RoadmapId = @Id ORDER BY SortOrder",
            new { Id = roadmapId })).ToList();

        var tasks = (await conn.QueryAsync<TaskRow>(
            """
            SELECT t.Id, t.MilestoneId, t.Title, t.Description, t.TaskType, t.ExternalUrl,
                   t.SortOrder, t.IsCompleted, t.CompletedAt, t.DueDate,
                   t.LinkedExamId, t.LinkedCourseId, t.LinkedSkillId
            FROM [recommendation].[Tasks] t
            JOIN [recommendation].[Milestones] m ON m.Id = t.MilestoneId
            WHERE m.RoadmapId = @Id
            ORDER BY t.SortOrder
            """,
            new { Id = roadmapId })).ToList();

        var tasksByMilestone = tasks
            .GroupBy(t => t.MilestoneId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TaskDto(
                    t.Id, t.Title, t.Description, t.TaskType, t.ExternalUrl,
                    t.SortOrder, t.IsCompleted, t.CompletedAt,
                    t.DueDate?.ToString("yyyy-MM-dd"),
                    t.LinkedExamId, t.LinkedCourseId, t.LinkedSkillId
                )).ToList());

        var milestoneDtos = milestones.Select(m => new MilestoneDto(
            m.Id, m.Title, m.Description, m.SortOrder, m.IsCompleted, m.CompletedAt,
            tasksByMilestone.TryGetValue(m.Id, out var tl) ? tl : new List<TaskDto>()
        )).ToList();

        return new RoadmapDetailDto(
            roadmap.Id, roadmap.Title, roadmap.Description, roadmap.Status,
            roadmap.CareerId, roadmap.CareerTitle,
            roadmap.TargetDate.HasValue ? (DateTimeOffset?)new DateTimeOffset(roadmap.TargetDate.Value, TimeSpan.Zero) : null,
            roadmap.CompletedAt, roadmap.CreatedAt,
            milestoneDtos);
    }

    public async Task<Guid> CreateRoadmapAsync(Guid userId, string title, string? description,
        Guid? careerId, DateOnly? targetDate, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [recommendation].[Roadmaps] (UserId, CareerId, Title, Description, TargetDate)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @CareerId, @Title, @Description, @TargetDate)
            """,
            new
            {
                UserId = userId, CareerId = careerId, Title = title,
                Description = description,
                TargetDate = targetDate.HasValue ? targetDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
            });
    }

    public async Task DeleteRoadmapAsync(Guid roadmapId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM [recommendation].[Roadmaps] WHERE Id = @Id AND UserId = @UserId",
            new { Id = roadmapId, UserId = userId });
    }

    // ── Milestones ────────────────────────────────────────────────────────────

    public async Task<int> AddMilestoneAsync(Guid roadmapId, string title, string? description,
        int sortOrder, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO [recommendation].[Milestones] (RoadmapId, Title, Description, SortOrder)
            OUTPUT INSERTED.Id
            VALUES (@RoadmapId, @Title, @Description, @SortOrder)
            """,
            new { RoadmapId = roadmapId, Title = title, Description = description, SortOrder = sortOrder });
    }

    public async Task CompleteMilestoneAsync(int milestoneId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE m SET m.IsCompleted = 1, m.CompletedAt = SYSUTCDATETIME()
            FROM [recommendation].[Milestones] m
            JOIN [recommendation].[Roadmaps] r ON r.Id = m.RoadmapId
            WHERE m.Id = @MilestoneId AND r.UserId = @UserId
            """,
            new { MilestoneId = milestoneId, UserId = userId });
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    public async Task<int> AddTaskAsync(int milestoneId, string title, string? description,
        string taskType, string? externalUrl, int sortOrder, DateOnly? dueDate,
        int? linkedExamId, int? linkedCourseId, int? linkedSkillId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO [recommendation].[Tasks]
                (MilestoneId, Title, Description, TaskType, ExternalUrl, SortOrder, DueDate,
                 LinkedExamId, LinkedCourseId, LinkedSkillId)
            OUTPUT INSERTED.Id
            VALUES (@MilestoneId, @Title, @Description, @TaskType, @ExternalUrl, @SortOrder, @DueDate,
                    @LinkedExamId, @LinkedCourseId, @LinkedSkillId)
            """,
            new
            {
                MilestoneId = milestoneId, Title = title, Description = description, TaskType = taskType,
                ExternalUrl = externalUrl, SortOrder = sortOrder,
                DueDate = dueDate.HasValue ? dueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                LinkedExamId = linkedExamId, LinkedCourseId = linkedCourseId, LinkedSkillId = linkedSkillId
            });
    }

    public async Task CompleteTaskAsync(int taskId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE t SET t.IsCompleted = 1, t.CompletedAt = SYSUTCDATETIME()
            FROM [recommendation].[Tasks] t
            JOIN [recommendation].[Milestones] m ON m.Id = t.MilestoneId
            JOIN [recommendation].[Roadmaps] r ON r.Id = m.RoadmapId
            WHERE t.Id = @TaskId AND r.UserId = @UserId
            """,
            new { TaskId = taskId, UserId = userId });
    }
}
