using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Export;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class ExportRepository : IExportRepository
{
    private readonly ISqlConnectionFactory _db;
    public ExportRepository(ISqlConnectionFactory db) => _db = db;

    private sealed record ExportJobDbRow(
        Guid Id, string ExportType, string Format, string Status,
        DateTimeOffset ExpireAt, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt,
        Guid DownloadToken, string? ErrorDetails);

    public async Task<Guid> CreateJobAsync(Guid userId, string exportType, string format, TimeSpan expiry, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var expireAt = DateTimeOffset.UtcNow.Add(expiry);

        return await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [export].[ExportJobs] (CreatedBy, ExportType, Format, ExpireAt, Status)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @ExportType, @Format, @ExpireAt, 'Pending')
            """,
            new { UserId = userId, ExportType = exportType, Format = format, ExpireAt = expireAt });
    }

    public async Task<ExportJobDto?> GetJobAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<ExportJobDbRow>(
            """
            SELECT Id, ExportType, Format, Status, ExpireAt, CreatedAt, CompletedAt, DownloadToken, ErrorDetails
            FROM [export].[ExportJobs]
            WHERE Id = @Id
            """, new { Id = id });

        return row is null ? null : MapToDto(row);
    }

    public async Task<IReadOnlyList<ExportJobDto>> ListJobsAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<ExportJobDbRow>(
            """
            SELECT Id, ExportType, Format, Status, ExpireAt, CreatedAt, CompletedAt, DownloadToken, ErrorDetails
            FROM [export].[ExportJobs]
            WHERE CreatedBy = @UserId
            ORDER BY CreatedAt DESC
            """, new { UserId = userId });

        return rows.Select(MapToDto).ToList();
    }

    public async Task UpdateJobStatusAsync(Guid id, string status, string? storedPath, string? errorDetails, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [export].[ExportJobs]
            SET Status = @Status,
                StoredPath = COALESCE(@StoredPath, StoredPath),
                ErrorDetails = COALESCE(@ErrorDetails, ErrorDetails),
                CompletedAt = CASE WHEN @Status IN ('Completed', 'Failed') THEN SYSUTCDATETIME() ELSE CompletedAt END
            WHERE Id = @Id
            """, new { Id = id, Status = status, StoredPath = storedPath, ErrorDetails = errorDetails });
    }

    public async Task<ExportJobDto?> GetJobByTokenAsync(Guid id, Guid downloadToken, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<ExportJobDbRow>(
            """
            SELECT Id, ExportType, Format, Status, ExpireAt, CreatedAt, CompletedAt, DownloadToken, ErrorDetails
            FROM [export].[ExportJobs]
            WHERE Id = @Id AND DownloadToken = @DownloadToken
            """, new { Id = id, DownloadToken = downloadToken });

        return row is null ? null : MapToDto(row);
    }

    private static ExportJobDto MapToDto(ExportJobDbRow row)
    {
        var downloadUrl = row.Status == "Completed"
            ? $"/api/v1/exports/download/{row.Id}?token={row.DownloadToken}"
            : null;

        return new ExportJobDto(
            row.Id, row.ExportType, row.Format, row.Status, row.ExpireAt,
            row.CreatedAt, row.CompletedAt, downloadUrl, row.ErrorDetails);
    }
}
