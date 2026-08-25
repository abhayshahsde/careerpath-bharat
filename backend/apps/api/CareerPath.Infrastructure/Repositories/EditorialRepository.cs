using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Editorial;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class EditorialRepository : IEditorialRepository
{
    private readonly ISqlConnectionFactory _db;
    public EditorialRepository(ISqlConnectionFactory db) => _db = db;

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<ArticleSummaryDto> Items, int Total)> GetArticlesAsync(
        string? status, string? articleType, string? locale, Guid? authorId,
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var where = "WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(status))      where += " AND a.Status = @Status";
        if (!string.IsNullOrWhiteSpace(articleType)) where += " AND a.ArticleType = @ArticleType";
        if (!string.IsNullOrWhiteSpace(locale))      where += " AND a.Locale = @Locale";
        if (authorId.HasValue)                        where += " AND a.AuthorId = @AuthorId";
        if (!string.IsNullOrWhiteSpace(search))      where += " AND (cv.Title LIKE @Search OR cv.Summary LIKE @Search)";

        var searchParam = $"%{search}%";
        var offset = (page - 1) * pageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(1)
            FROM [editorial].[Articles] a
            LEFT JOIN [editorial].[ContentVersions] cv ON cv.ArticleId = a.Id AND cv.IsCurrentVersion = 1
            {where}
            """,
            new { Status = status, ArticleType = articleType, Locale = locale, AuthorId = authorId, Search = searchParam });

        var items = await conn.QueryAsync<ArticleSummaryDto>(
            $"""
            SELECT a.Id, a.Slug, a.ArticleType, a.Status, a.Locale,
                   COALESCE(cv.Title, '') AS Title,
                   cv.Summary,
                   u.DisplayName AS AuthorName,
                   a.UpdatedAt,
                   a.PublishedAt
            FROM [editorial].[Articles] a
            LEFT JOIN [editorial].[ContentVersions] cv ON cv.ArticleId = a.Id AND cv.IsCurrentVersion = 1
            LEFT JOIN [identity].[Users] u ON u.Id = a.AuthorId
            {where}
            ORDER BY a.UpdatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """,
            new { Status = status, ArticleType = articleType, Locale = locale, AuthorId = authorId, Search = searchParam, Offset = offset, PageSize = pageSize });

        return (items.ToList(), total);
    }

    // ── Detail ────────────────────────────────────────────────────────────────

    public async Task<ArticleDetailDto?> GetArticleByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await FetchDetailAsync(conn, "a.Id = @Id", new { Id = id }, ct);
    }

    public async Task<ArticleDetailDto?> GetArticleBySlugAsync(string slug, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await FetchDetailAsync(conn, "a.Slug = @Slug AND a.Locale = @Locale", new { Slug = slug, Locale = locale }, ct);
    }

    private static async Task<ArticleDetailDto?> FetchDetailAsync(
        System.Data.IDbConnection conn, string predicate, object param, CancellationToken _)
    {
        var article = await conn.QuerySingleOrDefaultAsync(
            $"""
            SELECT a.Id, a.Slug, a.ArticleType, a.Status, a.Locale,
                   a.LinkedCareerId, a.AuthorId, a.AssignedEditorId,
                   a.ScheduledAt, a.PublishedAt, a.ArchivedAt, a.CreatedAt, a.UpdatedAt,
                   u.DisplayName AS AuthorName
            FROM [editorial].[Articles] a
            LEFT JOIN [identity].[Users] u ON u.Id = a.AuthorId
            WHERE {predicate}
            """, param);

        if (article is null) return null;

        var version = await conn.QuerySingleOrDefaultAsync<ContentVersionDto>(
            """
            SELECT cv.Id, cv.VersionNumber, cv.Title, cv.Summary, cv.Body,
                   cv.MetaDescription, cv.Keywords, cv.ChangeNote,
                   u.DisplayName AS CreatedByName, cv.CreatedAt,
                   cv.WordCount, cv.ReadingTimeMinutes
            FROM [editorial].[ContentVersions] cv
            LEFT JOIN [identity].[Users] u ON u.Id = cv.CreatedBy
            WHERE cv.ArticleId = @ArticleId AND cv.IsCurrentVersion = 1
            """, new { ArticleId = (Guid)article.Id });

        var reviews = await conn.QueryAsync<ReviewRequestDto>(
            """
            SELECT rr.Id, rr.Status, u.DisplayName AS ReviewerName,
                   rr.Feedback, rr.RequestedAt, rr.ReviewedAt, rr.DueBy
            FROM [editorial].[ReviewRequests] rr
            LEFT JOIN [identity].[Users] u ON u.Id = rr.ReviewerId
            WHERE rr.ArticleId = @ArticleId
            ORDER BY rr.RequestedAt DESC
            """, new { ArticleId = (Guid)article.Id });

        return new ArticleDetailDto(
            Id:               article.Id,
            Slug:             article.Slug,
            ArticleType:      article.ArticleType,
            Status:           article.Status,
            Locale:           article.Locale,
            LinkedCareerId:   article.LinkedCareerId,
            AuthorId:         article.AuthorId,
            AuthorName:       article.AuthorName,
            AssignedEditorId: article.AssignedEditorId,
            ScheduledAt:      article.ScheduledAt,
            PublishedAt:      article.PublishedAt,
            ArchivedAt:       article.ArchivedAt,
            CreatedAt:        article.CreatedAt,
            UpdatedAt:        article.UpdatedAt,
            CurrentVersion:   version,
            Reviews:          reviews.ToList());
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Guid> CreateArticleAsync(
        string slug, string articleType, string locale, Guid authorId,
        Guid? linkedCareerId, string title, string body, string? summary,
        string? metaDescription, string? keywords, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var articleId = await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [editorial].[Articles]
                (Slug, ArticleType, Status, Locale, LinkedCareerId, AuthorId, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Slug, @ArticleType, 'Draft', @Locale, @LinkedCareerId, @AuthorId, SYSUTCDATETIME(), SYSUTCDATETIME())
            """,
            new { Slug = slug, ArticleType = articleType, Locale = locale, LinkedCareerId = linkedCareerId, AuthorId = authorId },
            tx);

        var wordCount = body.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var readingTime = Math.Max(1, wordCount / 200);

        await conn.ExecuteAsync(
            """
            INSERT INTO [editorial].[ContentVersions]
                (ArticleId, VersionNumber, Title, Summary, Body, MetaDescription, Keywords,
                 CreatedBy, IsCurrentVersion, WordCount, ReadingTimeMinutes)
            VALUES
                (@ArticleId, 1, @Title, @Summary, @Body, @MetaDescription, @Keywords,
                 @AuthorId, 1, @WordCount, @ReadingTime)
            """,
            new { ArticleId = articleId, Title = title, Summary = summary, Body = body,
                  MetaDescription = metaDescription, Keywords = keywords, AuthorId = authorId,
                  WordCount = wordCount, ReadingTime = readingTime },
            tx);

        await LogEventAsync(conn, tx, articleId, authorId, "Created", null);
        tx.Commit();
        return articleId;
    }

    // ── Save Version ──────────────────────────────────────────────────────────

    public async Task<int> SaveVersionAsync(
        Guid articleId, Guid editorId, string title, string body, string? summary,
        string? metaDescription, string? keywords, string? changeNote, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // Unset current version flag on all previous
        await conn.ExecuteAsync(
            "UPDATE [editorial].[ContentVersions] SET IsCurrentVersion = 0 WHERE ArticleId = @ArticleId",
            new { ArticleId = articleId }, tx);

        var nextVersion = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(VersionNumber), 0) + 1 FROM [editorial].[ContentVersions] WHERE ArticleId = @ArticleId",
            new { ArticleId = articleId }, tx);

        var wordCount = body.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        var versionId = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO [editorial].[ContentVersions]
                (ArticleId, VersionNumber, Title, Summary, Body, MetaDescription, Keywords,
                 ChangeNote, CreatedBy, IsCurrentVersion, WordCount, ReadingTimeMinutes)
            OUTPUT INSERTED.Id
            VALUES (@ArticleId, @VersionNumber, @Title, @Summary, @Body, @MetaDescription, @Keywords,
                    @ChangeNote, @EditorId, 1, @WordCount, @ReadingTime)
            """,
            new { ArticleId = articleId, VersionNumber = nextVersion, Title = title,
                  Summary = summary, Body = body, MetaDescription = metaDescription,
                  Keywords = keywords, ChangeNote = changeNote, EditorId = editorId,
                  WordCount = wordCount, ReadingTime = Math.Max(1, wordCount / 200) },
            tx);

        await conn.ExecuteAsync(
            "UPDATE [editorial].[Articles] SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = articleId }, tx);

        await LogEventAsync(conn, tx, articleId, editorId, "VersionSaved",
            System.Text.Json.JsonSerializer.Serialize(new { VersionNumber = nextVersion }));

        tx.Commit();
        return versionId;
    }

    // ── Workflow ──────────────────────────────────────────────────────────────

    public async Task SubmitForReviewAsync(
        Guid articleId, Guid requesterId, Guid? reviewerId, DateTimeOffset? dueBy, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var currentVersionId = await conn.ExecuteScalarAsync<int>(
            "SELECT Id FROM [editorial].[ContentVersions] WHERE ArticleId = @Id AND IsCurrentVersion = 1",
            new { Id = articleId }, tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO [editorial].[ReviewRequests]
                (ArticleId, ContentVersionId, ReviewerId, Status, DueBy)
            VALUES (@ArticleId, @VersionId, @ReviewerId, 'Pending', @DueBy)
            """,
            new { ArticleId = articleId, VersionId = currentVersionId, ReviewerId = reviewerId, DueBy = dueBy }, tx);

        await conn.ExecuteAsync(
            "UPDATE [editorial].[Articles] SET Status = 'InReview', UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = articleId }, tx);

        await LogEventAsync(conn, tx, articleId, requesterId, "SubmittedForReview", null);
        tx.Commit();
    }

    public async Task RecordReviewDecisionAsync(
        Guid articleId, int reviewRequestId, Guid reviewerId,
        string decision, string? feedback, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var reviewStatus = decision switch
        {
            "Approve"        => "Approved",
            "RequestChanges" => "ChangesRequested",
            "Reject"         => "Rejected",
            _                => throw new InvalidOperationException($"Unknown decision: {decision}")
        };
        var articleStatus = decision switch
        {
            "Approve"        => "Approved",
            "RequestChanges" => "Draft",
            "Reject"         => "Draft",
            _                => "Draft"
        };

        await conn.ExecuteAsync(
            """
            UPDATE [editorial].[ReviewRequests]
            SET Status = @Status, Feedback = @Feedback, ReviewerId = @ReviewerId, ReviewedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new { Status = reviewStatus, Feedback = feedback, ReviewerId = reviewerId, Id = reviewRequestId }, tx);

        await conn.ExecuteAsync(
            "UPDATE [editorial].[Articles] SET Status = @Status, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Status = articleStatus, Id = articleId }, tx);

        await LogEventAsync(conn, tx, articleId, reviewerId, $"Review{decision}ed",
            feedback is not null ? System.Text.Json.JsonSerializer.Serialize(new { feedback }) : null);

        tx.Commit();
    }

    public async Task PublishArticleAsync(
        Guid articleId, Guid actorId, DateTimeOffset? scheduledAt, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var now = DateTimeOffset.UtcNow;
        var publishAt = scheduledAt ?? now;
        var status = scheduledAt > now ? "Scheduled" : "Published";

        await conn.ExecuteAsync(
            """
            UPDATE [editorial].[Articles]
            SET Status = @Status, ScheduledAt = @ScheduledAt, PublishedAt = CASE WHEN @ScheduledAt IS NULL THEN SYSUTCDATETIME() ELSE NULL END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new { Status = status, ScheduledAt = scheduledAt, Id = articleId }, tx);

        await LogEventAsync(conn, tx, articleId, actorId, "Published",
            System.Text.Json.JsonSerializer.Serialize(new { scheduledAt }));

        tx.Commit();
    }

    public async Task ArchiveArticleAsync(Guid articleId, Guid actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE [editorial].[Articles] SET Status = 'Archived', ArchivedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = articleId }, tx);

        await LogEventAsync(conn, tx, articleId, actorId, "Archived", null);
        tx.Commit();
    }

    public async Task<byte[]?> GetRowVersionAsync(Guid articleId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<byte[]?>(
            "SELECT RowVersion FROM [editorial].[Articles] WHERE Id = @Id",
            new { Id = articleId });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Task LogEventAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx,
        Guid articleId, Guid actorId, string eventType, string? payload)
    {
        return conn.ExecuteAsync(
            """
            INSERT INTO [editorial].[EditorialEvents] (ArticleId, ActorId, EventType, Payload)
            VALUES (@ArticleId, @ActorId, @EventType, @Payload)
            """,
            new { ArticleId = articleId, ActorId = actorId, EventType = eventType, Payload = payload },
            tx);
    }
}
