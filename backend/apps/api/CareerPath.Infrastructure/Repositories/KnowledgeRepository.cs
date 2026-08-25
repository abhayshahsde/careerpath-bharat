using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Knowledge;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class KnowledgeRepository : IKnowledgeRepository
{
    private readonly ISqlConnectionFactory _db;
    public KnowledgeRepository(ISqlConnectionFactory db) => _db = db;

    private sealed record DocumentDbRow(
        Guid Id, string Title, string DocType, string Status, string FilePath,
        long FileSize, string? ErrorDetails, Guid CreatedBy, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    private sealed record ChunkDbRow(
        long Id, Guid DocumentId, int ChunkIndex, string Content, int TokenCount, bool IsReviewed, string? VectorRefId);

    public async Task<Guid> CreateDocumentAsync(Guid userId, string title, string docType, string filePath, long fileSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [knowledge].[Documents] (CreatedBy, Title, DocType, FilePath, FileSize, Status)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Title, @DocType, @FilePath, @FileSize, 'Pending')
            """,
            new { UserId = userId, Title = title, DocType = docType, FilePath = filePath, FileSize = fileSize });
    }

    public async Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var doc = await conn.QuerySingleOrDefaultAsync<DocumentDbRow>(
            """
            SELECT Id, Title, DocType, Status, FilePath, FileSize, ErrorDetails, CreatedBy, CreatedAt, UpdatedAt
            FROM [knowledge].[Documents]
            WHERE Id = @Id
            """, new { Id = id });

        if (doc is null) return null;

        var chunks = await conn.QueryAsync<ChunkDbRow>(
            """
            SELECT Id, DocumentId, ChunkIndex, Content, TokenCount, IsReviewed, VectorRefId
            FROM [knowledge].[DocumentChunks]
            WHERE DocumentId = @DocId
            ORDER BY ChunkIndex
            """, new { DocId = id });

        return new DocumentDetailDto(
            doc.Id, doc.Title, doc.DocType, doc.Status, doc.FilePath, doc.FileSize, doc.ErrorDetails, doc.CreatedAt, doc.UpdatedAt,
            chunks.Select(c => new ChunkDto(c.Id, c.ChunkIndex, c.Content, c.TokenCount, c.IsReviewed, c.VectorRefId)).ToList());
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<DocumentDbRow>(
            """
            SELECT Id, Title, DocType, Status, FilePath, FileSize, ErrorDetails, CreatedBy, CreatedAt, UpdatedAt
            FROM [knowledge].[Documents]
            ORDER BY CreatedAt DESC
            """);

        return rows.Select(r => new DocumentSummaryDto(
            r.Id, r.Title, r.DocType, r.Status, r.FileSize, r.CreatedAt, r.UpdatedAt)).ToList();
    }

    public async Task UpdateDocumentStatusAsync(Guid id, string status, string? errorDetails, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [knowledge].[Documents]
            SET Status = @Status,
                ErrorDetails = COALESCE(@ErrorDetails, ErrorDetails),
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """, new { Id = id, Status = status, ErrorDetails = errorDetails });
    }

    public async Task BulkInsertChunksAsync(Guid documentId, IEnumerable<(int Index, string Content, int TokenCount)> chunks, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // Clear existing chunks first
        await conn.ExecuteAsync("DELETE FROM [knowledge].[DocumentChunks] WHERE DocumentId = @DocId", new { DocId = documentId }, tx);

        foreach (var c in chunks)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO [knowledge].[DocumentChunks] (DocumentId, ChunkIndex, Content, TokenCount)
                VALUES (@DocId, @Index, @Content, @TokenCount)
                """, new { DocId = documentId, Index = c.Index, Content = c.Content, TokenCount = c.TokenCount }, tx);
        }

        tx.Commit();
    }

    public async Task<ChunkDto?> GetChunkAsync(long chunkId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var c = await conn.QuerySingleOrDefaultAsync<ChunkDbRow>(
            """
            SELECT Id, DocumentId, ChunkIndex, Content, TokenCount, IsReviewed, VectorRefId
            FROM [knowledge].[DocumentChunks]
            WHERE Id = @Id
            """, new { Id = chunkId });

        return c is null ? null : new ChunkDto(c.Id, c.ChunkIndex, c.Content, c.TokenCount, c.IsReviewed, c.VectorRefId);
    }

    public async Task UpdateChunkAsync(long chunkId, string content, bool isReviewed, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [knowledge].[DocumentChunks]
            SET Content = @Content,
                IsReviewed = @IsReviewed,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """, new { Id = chunkId, Content = content, IsReviewed = isReviewed ? 1 : 0 });
    }

    public async Task UpdateChunkVectorRefAsync(long chunkId, string vectorRefId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [knowledge].[DocumentChunks]
            SET VectorRefId = @VectorRefId,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """, new { Id = chunkId, VectorRefId = vectorRefId });
    }

    public async Task SubmitReviewAsync(Guid documentId, Guid reviewerId, bool isApproved, string? notes, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            INSERT INTO [knowledge].[DocumentReviews] (DocumentId, ReviewedBy, IsApproved, Notes)
            VALUES (@DocumentId, @ReviewedBy, @IsApproved, @Notes)
            """, new { DocumentId = documentId, ReviewedBy = reviewerId, IsApproved = isApproved ? 1 : 0, Notes = notes });
    }
}
