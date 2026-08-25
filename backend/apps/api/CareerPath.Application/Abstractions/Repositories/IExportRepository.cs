using CareerPath.Contracts.V1.Export;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IExportRepository
{
    Task<Guid> CreateJobAsync(Guid userId, string exportType, string format, TimeSpan expiry, CancellationToken ct = default);
    Task<ExportJobDto?> GetJobAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExportJobDto>> ListJobsAsync(Guid userId, CancellationToken ct = default);
    Task UpdateJobStatusAsync(Guid id, string status, string? storedPath, string? errorDetails, CancellationToken ct = default);
    Task<ExportJobDto?> GetJobByTokenAsync(Guid id, Guid downloadToken, CancellationToken ct = default);
}
