using CareerPath.Contracts.V1.Knowledge;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IKnowledgeRepository
{
    Task<Guid> CreateDocumentAsync(Guid userId, string title, string docType, string filePath, long fileSize, CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(CancellationToken ct = default);
    Task UpdateDocumentStatusAsync(Guid id, string status, string? errorDetails, CancellationToken ct = default);
    Task BulkInsertChunksAsync(Guid documentId, IEnumerable<(int Index, string Content, int TokenCount)> chunks, CancellationToken ct = default);
    Task<ChunkDto?> GetChunkAsync(long chunkId, CancellationToken ct = default);
    Task UpdateChunkAsync(long chunkId, string content, bool isReviewed, CancellationToken ct = default);
    Task UpdateChunkVectorRefAsync(long chunkId, string vectorRefId, CancellationToken ct = default);
    Task SubmitReviewAsync(Guid documentId, Guid reviewerId, bool isApproved, string? notes, CancellationToken ct = default);
}
