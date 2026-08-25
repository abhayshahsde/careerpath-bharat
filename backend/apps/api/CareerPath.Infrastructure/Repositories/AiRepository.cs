using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Ai;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class AiRepository : IAiRepository
{
    private readonly ISqlConnectionFactory _db;
    public AiRepository(ISqlConnectionFactory db) => _db = db;

    private sealed record QuotaDbRow(
        Guid UserId, int MaxDailyTokens, int UsedDailyTokens, DateTimeOffset ResetAt, DateTimeOffset UpdatedAt);

    private sealed record CitationDbRow(
        Guid DocumentId, string DocumentTitle, string DocType, int ChunkIndex, string Content);

    public async Task<QuotaStatusDto> GetOrCreateQuotaAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var quota = await conn.QuerySingleOrDefaultAsync<QuotaDbRow>(
            "SELECT UserId, MaxDailyTokens, UsedDailyTokens, ResetAt, UpdatedAt FROM [ai].[UserQuotas] WHERE UserId = @UserId",
            new { UserId = userId });

        var now = DateTimeOffset.UtcNow;

        if (quota is null)
        {
            var resetAt = now.AddDays(1);
            await conn.ExecuteAsync(
                """
                INSERT INTO [ai].[UserQuotas] (UserId, MaxDailyTokens, UsedDailyTokens, ResetAt, UpdatedAt)
                VALUES (@UserId, 50000, 0, @ResetAt, SYSUTCDATETIME())
                """,
                new { UserId = userId, ResetAt = resetAt });

            return new QuotaStatusDto(50000, 0, resetAt);
        }

        if (now > quota.ResetAt)
        {
            var nextReset = now.AddDays(1);
            await conn.ExecuteAsync(
                """
                UPDATE [ai].[UserQuotas]
                SET UsedDailyTokens = 0,
                    ResetAt = @NextReset,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId
                """,
                new { UserId = userId, NextReset = nextReset });

            return new QuotaStatusDto(quota.MaxDailyTokens, 0, nextReset);
        }

        return new QuotaStatusDto(quota.MaxDailyTokens, quota.UsedDailyTokens, quota.ResetAt);
    }

    public async Task<bool> CheckAndConsumeQuotaAsync(Guid userId, int tokens, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var q = await GetOrCreateQuotaAsync(userId, ct);

        if (q.UsedDailyTokens + tokens > q.MaxDailyTokens)
        {
            return false;
        }

        await conn.ExecuteAsync(
            """
            UPDATE [ai].[UserQuotas]
            SET UsedDailyTokens = UsedDailyTokens + @Tokens,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId
            """,
            new { UserId = userId, Tokens = tokens });

        return true;
    }

    public async Task LogUsageAsync(Guid userId, string requestType, string modelName, int promptTokens, int completionTokens, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            INSERT INTO [ai].[UsageLogs] (UserId, RequestType, ModelName, PromptTokens, CompletionTokens)
            VALUES (@UserId, @RequestType, @ModelName, @PromptTokens, @CompletionTokens)
            """,
            new { UserId = userId, RequestType = requestType, ModelName = modelName, PromptTokens = promptTokens, CompletionTokens = completionTokens });
    }

    public async Task<IReadOnlyList<CitationDto>> SearchKnowledgeBaseAsync(string queryText, int maxResults = 3, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        // Simple sentence match search
        var searchLike = $"%{queryText}%";

        var rows = await conn.QueryAsync<CitationDbRow>(
            """
            SELECT TOP (@MaxResults)
                   c.DocumentId, d.Title AS DocumentTitle, d.DocType, c.ChunkIndex, c.Content
            FROM [knowledge].[DocumentChunks] c
            JOIN [knowledge].[Documents] d ON d.Id = c.DocumentId
            WHERE d.Status = 'Indexed' AND (c.Content LIKE @Search OR d.Title LIKE @Search)
            ORDER BY c.ChunkIndex ASC
            """,
            new { Search = searchLike, MaxResults = maxResults });

        return rows.Select(r => new CitationDto(r.DocumentId, r.DocumentTitle, r.DocType, r.ChunkIndex, r.Content)).ToList();
    }
}
