namespace CareerPath.Contracts.V1.Catalog;

// ── Categories ────────────────────────────────────────────────────────────────

public sealed record CategoryDto(
    string Id,
    string Name,
    string? ParentId,
    int SortOrder);

// ── Skills ────────────────────────────────────────────────────────────────────

public sealed record SkillDto(
    int Id,
    string Name,
    string Slug,
    string? Category);

// ── Exams ─────────────────────────────────────────────────────────────────────

public sealed record ExamDto(
    int Id,
    string Slug,
    string Name,
    string? FullName,
    string? ConductingBody,
    string? Level,
    string? Frequency,
    string? Description,
    string? OfficialUrl);

public sealed record ExamSummaryDto(
    int Id,
    string Slug,
    string Name,
    string? ConductingBody,
    string? Level);

// ── Courses ───────────────────────────────────────────────────────────────────

public sealed record CourseDto(
    int Id,
    string Slug,
    string Name,
    string? ShortName,
    string DegreeLevel,
    decimal DurationYears,
    string? CategoryId,
    string? Description);

public sealed record CourseSummaryDto(
    int Id,
    string Slug,
    string Name,
    string? ShortName,
    string DegreeLevel,
    decimal DurationYears);

// ── Scholarships ──────────────────────────────────────────────────────────────

public sealed record ScholarshipDto(
    int Id,
    string Slug,
    string Name,
    string ProviderName,
    string? Level,
    string? AmountLabel,
    string? EligibilitySummary,
    string? OfficialUrl,
    string? Disclaimer);

// ── Career Detail (enriched) ──────────────────────────────────────────────────

public sealed record CareerDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    string? Description,
    string? CategoryId,
    string? CategoryName,
    bool IsFeatured,
    string? SalaryRangeLabel,
    int MinEducationYears,
    int MaxEducationYears,
    string? ImageUrl,
    string? Disclaimer,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<ExamSummaryDto> Exams,
    IReadOnlyList<CourseSummaryDto> Courses);

// ── Catalog query filters ─────────────────────────────────────────────────────

public sealed record ExamListRequest(
    string? Level = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CourseListRequest(
    string? DegreeLevel = null,
    string? CategoryId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ScholarshipListRequest(
    string? Level = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
