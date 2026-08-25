namespace CareerPath.Contracts.V1.Import;

public sealed record UploadUrlRequest(
    string ImportType,
    string FileName,
    string ContentType,
    long FileSize);

public sealed record UploadUrlResponse(
    Guid JobId,
    string UploadUrl,
    string StoredPath);

public sealed record CreateImportJobRequest(
    Guid JobId);

public sealed record ImportJobSummaryDto(
    Guid Id,
    string ImportType,
    string Status,
    string FileName,
    long FileSize,
    int TotalRows,
    int ValidRows,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? CompletedAt);

public sealed record ImportJobDetailDto(
    Guid Id,
    string ImportType,
    string Status,
    string FileName,
    long FileSize,
    int TotalRows,
    int ValidRows,
    string? ErrorSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<StagedRowDto> StagedRows);

public sealed record StagedRowDto(
    long Id,
    int RowIndex,
    string RowData, // JSON string of row
    string RowStatus,
    string? ErrorMessage);

public sealed record ReviewImportRequest(
    bool IsApproved,
    string? Notes);
