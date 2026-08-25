namespace CareerPath.Contracts.V1.Editorial;

// ── Articles ──────────────────────────────────────────────────────────────────

public sealed record ArticleSummaryDto(
    Guid Id,
    string Slug,
    string ArticleType,
    string Status,
    string Locale,
    string Title,
    string? Summary,
    string? AuthorName,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record ArticleDetailDto(
    Guid Id,
    string Slug,
    string ArticleType,
    string Status,
    string Locale,
    Guid? LinkedCareerId,
    Guid AuthorId,
    string? AuthorName,
    Guid? AssignedEditorId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ContentVersionDto? CurrentVersion,
    IReadOnlyList<ReviewRequestDto> Reviews);

public sealed record ContentVersionDto(
    int Id,
    int VersionNumber,
    string Title,
    string? Summary,
    string Body,
    string? MetaDescription,
    string? Keywords,
    string? ChangeNote,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int WordCount,
    int ReadingTimeMinutes);

public sealed record ReviewRequestDto(
    int Id,
    string Status,
    string? ReviewerName,
    string? Feedback,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? DueBy);

// ── Requests ──────────────────────────────────────────────────────────────────

public sealed record CreateArticleRequest(
    string Slug,
    string ArticleType,
    string Locale,
    string Title,
    string Body,
    string? Summary = null,
    string? MetaDescription = null,
    string? Keywords = null,
    Guid? LinkedCareerId = null);

public sealed record SaveVersionRequest(
    string Title,
    string Body,
    string? Summary = null,
    string? MetaDescription = null,
    string? Keywords = null,
    string? ChangeNote = null);

public sealed record SubmitForReviewRequest(
    Guid? ReviewerId = null,
    DateTimeOffset? DueBy = null);

public sealed record ReviewDecisionRequest(
    string Decision,     // 'Approve' | 'RequestChanges' | 'Reject'
    string? Feedback = null);

public sealed record PublishArticleRequest(
    DateTimeOffset? ScheduledAt = null);  // null = publish immediately

public sealed record ArticleListRequest(
    string? Status = null,
    string? ArticleType = null,
    string? Locale = null,
    Guid? AuthorId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
