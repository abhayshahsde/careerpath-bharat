using Dapper;
using System.Text.Json;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Import;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class ImportRepository : IImportRepository
{
    private readonly ISqlConnectionFactory _db;
    public ImportRepository(ISqlConnectionFactory db) => _db = db;

    // ── Private strongly-typed record definitions for DB rows ────────────────

    private sealed record ImportJobDbRow(
        Guid Id, string ImportType, string Status, string FileName, string StoredPath,
        string? ContentType, long FileSize, int TotalRows, int ValidRows, string? ErrorSummary,
        Guid CreatedBy, DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt, DateTimeOffset? CompletedAt);

    private sealed record StagedRowDbRow(
        long Id, Guid JobId, int RowIndex, string RowData, string RowStatus, string? ErrorMessage);

    // ── Repository Implementations ────────────────────────────────────────────

    public async Task<Guid> CreateJobAsync(Guid userId, string importType, string fileName, string storedPath, string contentType, long fileSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [import].[ImportJobs] (CreatedBy, ImportType, FileName, StoredPath, ContentType, FileSize, Status)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @ImportType, @FileName, @StoredPath, @ContentType, @FileSize, 'Created')
            """,
            new { UserId = userId, ImportType = importType, FileName = fileName, StoredPath = storedPath, ContentType = contentType, FileSize = fileSize });
    }

    public async Task<ImportJobDetailDto?> GetJobAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var job = await conn.QuerySingleOrDefaultAsync<ImportJobDbRow>(
            """
            SELECT Id, ImportType, Status, FileName, StoredPath, ContentType, FileSize, 
                   TotalRows, ValidRows, ErrorSummary, CreatedBy, CreatedAt, ProcessedAt, CompletedAt 
            FROM [import].[ImportJobs] 
            WHERE Id = @Id
            """, new { Id = id });

        if (job is null) return null;

        var stagedRows = await conn.QueryAsync<StagedRowDbRow>(
            "SELECT Id, JobId, RowIndex, RowData, RowStatus, ErrorMessage FROM [import].[ImportStaging] WHERE JobId = @JobId ORDER BY RowIndex", new { JobId = id });

        return new ImportJobDetailDto(
            job.Id, job.ImportType, job.Status, job.FileName, job.FileSize,
            job.TotalRows, job.ValidRows, job.ErrorSummary, job.CreatedAt, job.ProcessedAt, job.CompletedAt,
            stagedRows.Select(r => new StagedRowDto(r.Id, r.RowIndex, r.RowData, r.RowStatus, r.ErrorMessage)).ToList());
    }

    public async Task<IReadOnlyList<ImportJobSummaryDto>> ListJobsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<ImportJobDbRow>(
            """
            SELECT Id, ImportType, Status, FileName, StoredPath, ContentType, FileSize, 
                   TotalRows, ValidRows, ErrorSummary, CreatedBy, CreatedAt, ProcessedAt, CompletedAt
            FROM [import].[ImportJobs] 
            ORDER BY CreatedAt DESC
            """);

        return rows.Select(r => new ImportJobSummaryDto(
            r.Id, r.ImportType, r.Status, r.FileName, r.FileSize, r.TotalRows, r.ValidRows, r.CreatedAt, r.ProcessedAt, r.CompletedAt)).ToList();
    }

    public async Task UpdateJobStatusAsync(Guid id, string status, int totalRows = 0, int validRows = 0, string? errorSummary = null, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [import].[ImportJobs]
            SET Status = @Status,
                TotalRows = CASE WHEN @TotalRows > 0 THEN @TotalRows ELSE TotalRows END,
                ValidRows = CASE WHEN @ValidRows > 0 THEN @ValidRows ELSE ValidRows END,
                ErrorSummary = COALESCE(@ErrorSummary, ErrorSummary),
                ProcessedAt = CASE WHEN @Status IN ('Staged', 'FailedValidation') THEN SYSUTCDATETIME() ELSE ProcessedAt END,
                CompletedAt = CASE WHEN @Status IN ('Completed', 'Failed') THEN SYSUTCDATETIME() ELSE CompletedAt END
            WHERE Id = @Id
            """,
            new { Id = id, Status = status, TotalRows = totalRows, ValidRows = validRows, ErrorSummary = errorSummary });
    }

    public async Task BulkInsertStagingRowsAsync(Guid jobId, IEnumerable<(int Index, string Data, string Status, string? Error)> rows, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // Clear existing staging rows for this job to avoid duplicates
        await conn.ExecuteAsync("DELETE FROM [import].[ImportStaging] WHERE JobId = @JobId", new { JobId = jobId }, tx);

        foreach (var r in rows)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO [import].[ImportStaging] (JobId, RowIndex, RowData, RowStatus, ErrorMessage)
                VALUES (@JobId, @RowIndex, @RowData, @RowStatus, @ErrorMessage)
                """,
                new { JobId = jobId, RowIndex = r.Index, RowData = r.Data, RowStatus = r.Status, ErrorMessage = r.Error }, tx);
        }

        tx.Commit();
    }

    public async Task SubmitReviewAsync(Guid jobId, Guid reviewerId, bool isApproved, string? notes, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO [import].[ImportReviews] (JobId, ReviewedBy, IsApproved, Notes)
            VALUES (@JobId, @ReviewedBy, @IsApproved, @Notes)
            """,
            new { JobId = jobId, ReviewedBy = reviewerId, IsApproved = isApproved, Notes = notes });
    }

    public async Task<bool> ApplyImportJobAsync(Guid jobId, Guid reviewerId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var job = await conn.QuerySingleOrDefaultAsync<ImportJobDbRow>(
            """
            SELECT Id, ImportType, Status, FileName, StoredPath, ContentType, FileSize, 
                   TotalRows, ValidRows, ErrorSummary, CreatedBy, CreatedAt, ProcessedAt, CompletedAt 
            FROM [import].[ImportJobs] 
            WHERE Id = @Id
            """, new { Id = jobId });

        if (job is null || (job.Status != "Staged" && job.Status != "Importing"))
        {
            return false;
        }

        var validRows = await conn.QueryAsync<StagedRowDbRow>(
            "SELECT Id, JobId, RowIndex, RowData, RowStatus, ErrorMessage FROM [import].[ImportStaging] WHERE JobId = @JobId AND RowStatus = 'Valid'", new { JobId = jobId });

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var row in validRows)
            {
                var doc = JsonDocument.Parse(row.RowData);
                var root = doc.RootElement;

                if (job.ImportType.Equals("Careers", StringComparison.OrdinalIgnoreCase))
                {
                    var slug = root.GetProperty("slug").GetString()!;
                    var categoryId = root.TryGetProperty("categoryId", out var c) ? c.GetString() : null;
                    var minEdu = root.TryGetProperty("minEducationYears", out var mn) && mn.ValueKind == JsonValueKind.Number ? mn.GetInt32() : 0;
                    var maxEdu = root.TryGetProperty("maxEducationYears", out var mx) && mx.ValueKind == JsonValueKind.Number ? mx.GetInt32() : 0;
                    var salary = root.TryGetProperty("salaryRangeLabel", out var s) ? s.GetString() : null;
                    var title = root.GetProperty("title").GetString()!;
                    var summary = root.TryGetProperty("summary", out var sm) ? sm.GetString() : null;
                    var description = root.TryGetProperty("description", out var ds) ? ds.GetString() : null;

                    // Merge into Careers
                    var careerId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                        """
                        MERGE [catalog].[Careers] AS target
                        USING (SELECT @Slug AS Slug) AS source
                        ON target.Slug = source.Slug
                        WHEN MATCHED THEN
                            UPDATE SET CategoryId = @CategoryId, MinEducationYears = @MinEdu, MaxEducationYears = @MaxEdu, SalaryRangeLabel = @Salary, UpdatedAt = SYSUTCDATETIME()
                        WHEN NOT MATCHED THEN
                            INSERT (Slug, CategoryId, MinEducationYears, MaxEducationYears, SalaryRangeLabel)
                            VALUES (@Slug, @CategoryId, @MinEdu, @MaxEdu, @Salary)
                        OUTPUT inserted.Id;
                        """,
                        new { Slug = slug, CategoryId = categoryId, MinEdu = minEdu, MaxEdu = maxEdu, Salary = salary }, tx);

                    if (careerId.HasValue)
                    {
                        // Merge CareerTranslations (en)
                        await conn.ExecuteAsync(
                            """
                            MERGE [catalog].[CareerTranslations] AS target
                            USING (SELECT @CareerId AS CareerId, 'en' AS Locale) AS source
                            ON target.CareerId = source.CareerId AND target.Locale = source.Locale
                            WHEN MATCHED THEN
                                UPDATE SET Title = @Title, Summary = @Summary, Description = @Description, UpdatedAt = SYSUTCDATETIME()
                            WHEN NOT MATCHED THEN
                                INSERT (CareerId, Locale, Title, Summary, Description)
                                VALUES (@CareerId, 'en', @Title, @Summary, @Description);
                            """,
                            new { CareerId = careerId.Value, Title = title, Summary = summary, Description = description }, tx);
                    }
                }
                else if (job.ImportType.Equals("Exams", StringComparison.OrdinalIgnoreCase))
                {
                    var slug = root.GetProperty("slug").GetString()!;
                    var name = root.GetProperty("name").GetString()!;
                    var fullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() : null;
                    var body = root.TryGetProperty("conductingBody", out var cb) ? cb.GetString() : null;
                    var level = root.TryGetProperty("level", out var lv) ? lv.GetString() : null;
                    var freq = root.TryGetProperty("frequency", out var fq) ? fq.GetString() : null;
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var url = root.TryGetProperty("officialUrl", out var ou) ? ou.GetString() : null;

                    await conn.ExecuteAsync(
                        """
                        MERGE [catalog].[Exams] AS target
                        USING (SELECT @Slug AS Slug) AS source
                        ON target.Slug = source.Slug
                        WHEN MATCHED THEN
                            UPDATE SET Name = @Name, FullName = @FullName, ConductingBody = @Body, Level = @Level, Frequency = @Freq, Description = @Desc, OfficialUrl = @Url
                        WHEN NOT MATCHED THEN
                            INSERT (Slug, Name, FullName, ConductingBody, Level, Frequency, Description, OfficialUrl)
                            VALUES (@Slug, @Name, @FullName, @Body, @Level, @Freq, @Desc, @Url);
                        """,
                        new { Slug = slug, Name = name, FullName = fullName, Body = body, Level = level, Freq = freq, Desc = desc, Url = url }, tx);
                }
                else if (job.ImportType.Equals("Courses", StringComparison.OrdinalIgnoreCase))
                {
                    var slug = root.GetProperty("slug").GetString()!;
                    var name = root.GetProperty("name").GetString()!;
                    var shortName = root.TryGetProperty("shortName", out var sn) ? sn.GetString() : null;
                    var level = root.GetProperty("degreeLevel").GetString()!;
                    var dur = root.TryGetProperty("durationYears", out var dy) && dy.ValueKind == JsonValueKind.Number ? dy.GetDecimal() : 3.0m;
                    var catId = root.TryGetProperty("categoryId", out var ci) ? ci.GetString() : null;
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;

                    await conn.ExecuteAsync(
                        """
                        MERGE [catalog].[Courses] AS target
                        USING (SELECT @Slug AS Slug) AS source
                        ON target.Slug = source.Slug
                        WHEN MATCHED THEN
                            UPDATE SET Name = @Name, ShortName = @ShortName, DegreeLevel = @Level, DurationYears = @Dur, CategoryId = @CatId, Description = @Desc
                        WHEN NOT MATCHED THEN
                            INSERT (Slug, Name, ShortName, DegreeLevel, DurationYears, CategoryId, Description)
                            VALUES (@Slug, @Name, @ShortName, @Level, @Dur, @CatId, @Desc);
                        """,
                        new { Slug = slug, Name = name, ShortName = shortName, Level = level, Dur = dur, CatId = catId, Desc = desc }, tx);
                }
                else if (job.ImportType.Equals("Scholarships", StringComparison.OrdinalIgnoreCase))
                {
                    var slug = root.GetProperty("slug").GetString()!;
                    var name = root.GetProperty("name").GetString()!;
                    var provider = root.GetProperty("providerName").GetString()!;
                    var level = root.TryGetProperty("level", out var lv) ? lv.GetString() : null;
                    var amount = root.TryGetProperty("amountLabel", out var al) ? al.GetString() : null;
                    var eligibility = root.TryGetProperty("eligibilitySummary", out var es) ? es.GetString() : null;
                    var url = root.TryGetProperty("officialUrl", out var ou) ? ou.GetString() : null;
                    var disclaimer = root.TryGetProperty("disclaimer", out var dc) ? dc.GetString() : null;

                    await conn.ExecuteAsync(
                        """
                        MERGE [catalog].[Scholarships] AS target
                        USING (SELECT @Slug AS Slug) AS source
                        ON target.Slug = source.Slug
                        WHEN MATCHED THEN
                            UPDATE SET Name = @Name, ProviderName = @Provider, Level = @Level, AmountLabel = @Amount, EligibilitySummary = @Eligibility, OfficialUrl = @Url, Disclaimer = @Disclaimer
                        WHEN NOT MATCHED THEN
                            INSERT (Slug, Name, ProviderName, Level, AmountLabel, EligibilitySummary, OfficialUrl, Disclaimer)
                            VALUES (@Slug, @Name, @Provider, @Level, @Amount, @Eligibility, @Url, @Disclaimer);
                        """,
                        new { Slug = slug, Name = name, Provider = provider, Level = level, Amount = amount, Eligibility = eligibility, Url = url, Disclaimer = disclaimer }, tx);
                }
            }

            // Update status to Completed
            await conn.ExecuteAsync(
                "UPDATE [import].[ImportJobs] SET Status = 'Completed', CompletedAt = SYSUTCDATETIME() WHERE Id = @Id",
                new { Id = jobId }, tx);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
