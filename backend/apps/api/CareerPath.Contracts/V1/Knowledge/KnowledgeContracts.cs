namespace CareerPath.Contracts.V1.Knowledge;

public sealed record UploadDocumentRequest(
    string Title,
    string DocType,
    string FileName,
    long FileSize);

public sealed record UploadDocumentResponse(
    Guid DocumentId,
    string UploadUrl,
    string FilePath);

public sealed record DocumentSummaryDto(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    long FileSize,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentDetailDto(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    string FilePath,
    long FileSize,
    string? ErrorDetails,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ChunkDto> Chunks);

public sealed record ChunkDto(
    long Id,
    int ChunkIndex,
    string Content,
    int TokenCount,
    bool IsReviewed,
    string? VectorRefId);

public sealed record UpdateChunkRequest(
    string Content,
    bool IsReviewed);

public sealed record ReviewDocumentRequest(
    bool IsApproved,
    string? Notes);
