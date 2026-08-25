using CareerPath.Contracts.V1.Common;
using CareerPath.Contracts.V1.Editorial;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IEditorialRepository
{
    // Queries
    Task<(IReadOnlyList<ArticleSummaryDto> Items, int Total)> GetArticlesAsync(
        string? status, string? articleType, string? locale, Guid? authorId,
        string? search, int page, int pageSize, CancellationToken ct = default);

    Task<ArticleDetailDto?> GetArticleByIdAsync(Guid id, CancellationToken ct = default);
    Task<ArticleDetailDto?> GetArticleBySlugAsync(string slug, string locale, CancellationToken ct = default);

    // Commands
    Task<Guid> CreateArticleAsync(
        string slug, string articleType, string locale,
        Guid authorId, Guid? linkedCareerId,
        string title, string body, string? summary,
        string? metaDescription, string? keywords,
        CancellationToken ct = default);

    Task<int> SaveVersionAsync(
        Guid articleId, Guid editorId,
        string title, string body, string? summary,
        string? metaDescription, string? keywords, string? changeNote,
        CancellationToken ct = default);

    // Workflow transitions
    Task SubmitForReviewAsync(Guid articleId, Guid requesterId, Guid? reviewerId, DateTimeOffset? dueBy, CancellationToken ct = default);
    Task RecordReviewDecisionAsync(Guid articleId, int reviewRequestId, Guid reviewerId, string decision, string? feedback, CancellationToken ct = default);
    Task PublishArticleAsync(Guid articleId, Guid actorId, DateTimeOffset? scheduledAt, CancellationToken ct = default);
    Task ArchiveArticleAsync(Guid articleId, Guid actorId, CancellationToken ct = default);

    // Rowversion check for optimistic concurrency
    Task<byte[]?> GetRowVersionAsync(Guid articleId, CancellationToken ct = default);
}
