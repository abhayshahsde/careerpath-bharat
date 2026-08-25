using CareerPath.Contracts.V1.Import;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IImportRepository
{
    Task<Guid> CreateJobAsync(Guid userId, string importType, string fileName, string storedPath, string contentType, long fileSize, CancellationToken ct = default);
    Task<ImportJobDetailDto?> GetJobAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImportJobSummaryDto>> ListJobsAsync(CancellationToken ct = default);
    Task UpdateJobStatusAsync(Guid id, string status, int totalRows = 0, int validRows = 0, string? errorSummary = null, CancellationToken ct = default);
    Task BulkInsertStagingRowsAsync(Guid jobId, IEnumerable<(int Index, string Data, string Status, string? Error)> rows, CancellationToken ct = default);
    Task SubmitReviewAsync(Guid jobId, Guid reviewerId, bool isApproved, string? notes, CancellationToken ct = default);
    Task<bool> ApplyImportJobAsync(Guid jobId, Guid reviewerId, CancellationToken ct = default);
}
