using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Catalog;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly ISqlConnectionFactory _db;
    public CatalogRepository(ISqlConnectionFactory db) => _db = db;

    // ── Categories ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CategoryDto>(
            """
            SELECT Id, Name, ParentId, SortOrder
            FROM [catalog].[Categories]
            WHERE IsActive = 1
            ORDER BY SortOrder
            """);
        return rows.ToList();
    }

    // ── Skills ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SkillDto>(
            """
            SELECT Id, Name, Slug, Category
            FROM [catalog].[Skills]
            WHERE IsActive = 1
            ORDER BY Category, Name
            """);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SkillDto>> GetCareerSkillsAsync(Guid careerId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SkillDto>(
            """
            SELECT s.Id, s.Name, s.Slug, s.Category
            FROM [catalog].[CareerSkills] cs
            JOIN [catalog].[Skills] s ON cs.SkillId = s.Id
            WHERE cs.CareerId = @CareerId AND s.IsActive = 1
            ORDER BY cs.IsRequired DESC, s.Category, s.Name
            """,
            new { CareerId = careerId });
        return rows.ToList();
    }

    // ── Exams ─────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<ExamDto> Items, int Total)> GetExamsAsync(
        string locale, string? level, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var where = "WHERE e.IsActive = 1";
        var param = new DynamicParameters();
        param.Add("Locale", locale);
        param.Add("Offset", (page - 1) * pageSize);
        param.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(level))
        {
            where += " AND e.Level = @Level";
            param.Add("Level", level);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (COALESCE(et.Name, e.Name) LIKE @Search OR COALESCE(et.FullName, e.FullName) LIKE @Search OR e.ConductingBody LIKE @Search)";
            param.Add("Search", $"%{search}%");
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(1)
            FROM [catalog].[Exams] e
            LEFT JOIN [catalog].[ExamTranslations] et ON et.ExamId = e.Id AND et.Locale = @Locale
            {where}
            """, param);

        var items = await conn.QueryAsync<ExamDto>(
            $"""
            SELECT
                e.Id,
                e.Slug,
                COALESCE(et.Name, e.Name) AS Name,
                COALESCE(et.FullName, e.FullName) AS FullName,
                e.ConductingBody,
                e.Level,
                e.Frequency,
                COALESCE(et.Description, e.Description) AS Description,
                e.OfficialUrl
            FROM [catalog].[Exams] e
            LEFT JOIN [catalog].[ExamTranslations] et ON et.ExamId = e.Id AND et.Locale = @Locale
            {where}
            ORDER BY COALESCE(et.Name, e.Name)
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, param);

        return (items.ToList(), total);
    }

    public async Task<ExamDto?> GetExamBySlugAsync(string slug, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ExamDto>(
            """
            SELECT
                e.Id,
                e.Slug,
                COALESCE(et.Name, e.Name) AS Name,
                COALESCE(et.FullName, e.FullName) AS FullName,
                e.ConductingBody,
                e.Level,
                e.Frequency,
                COALESCE(et.Description, e.Description) AS Description,
                e.OfficialUrl
            FROM [catalog].[Exams] e
            LEFT JOIN [catalog].[ExamTranslations] et ON et.ExamId = e.Id AND et.Locale = @Locale
            WHERE e.Slug = @Slug AND e.IsActive = 1
            """,
            new { Slug = slug, Locale = locale });
    }

    public async Task<IReadOnlyList<ExamSummaryDto>> GetCareerExamsAsync(Guid careerId, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<ExamSummaryDto>(
            """
            SELECT
                e.Id,
                e.Slug,
                COALESCE(et.Name, e.Name) AS Name,
                e.ConductingBody,
                e.Level
            FROM [catalog].[CareerExams] ce
            JOIN [catalog].[Exams] e ON ce.ExamId = e.Id
            LEFT JOIN [catalog].[ExamTranslations] et ON et.ExamId = e.Id AND et.Locale = @Locale
            WHERE ce.CareerId = @CareerId AND e.IsActive = 1
            ORDER BY ce.SortOrder, COALESCE(et.Name, e.Name)
            """,
            new { CareerId = careerId, Locale = locale });
        return rows.ToList();
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<CourseDto> Items, int Total)> GetCoursesAsync(
        string locale, string? degreeLevel, string? categoryId, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var where = "WHERE c.IsActive = 1";
        var param = new DynamicParameters();
        param.Add("Locale", locale);
        param.Add("Offset", (page - 1) * pageSize);
        param.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(degreeLevel))
        {
            where += " AND c.DegreeLevel = @DegreeLevel";
            param.Add("DegreeLevel", degreeLevel);
        }
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            where += " AND c.CategoryId = @CategoryId";
            param.Add("CategoryId", categoryId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (COALESCE(ct.Name, c.Name) LIKE @Search OR c.ShortName LIKE @Search)";
            param.Add("Search", $"%{search}%");
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(1)
            FROM [catalog].[Courses] c
            LEFT JOIN [catalog].[CourseTranslations] ct ON ct.CourseId = c.Id AND ct.Locale = @Locale
            {where}
            """, param);

        var items = await conn.QueryAsync<CourseDto>(
            $"""
            SELECT
                c.Id,
                c.Slug,
                COALESCE(ct.Name, c.Name) AS Name,
                c.ShortName,
                c.DegreeLevel,
                c.DurationYears,
                c.CategoryId,
                COALESCE(ct.Description, c.Description) AS Description
            FROM [catalog].[Courses] c
            LEFT JOIN [catalog].[CourseTranslations] ct ON ct.CourseId = c.Id AND ct.Locale = @Locale
            {where}
            ORDER BY c.DegreeLevel, COALESCE(ct.Name, c.Name)
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, param);

        return (items.ToList(), total);
    }

    public async Task<CourseDto?> GetCourseBySlugAsync(string slug, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<CourseDto>(
            """
            SELECT
                c.Id,
                c.Slug,
                COALESCE(ct.Name, c.Name) AS Name,
                c.ShortName,
                c.DegreeLevel,
                c.DurationYears,
                c.CategoryId,
                COALESCE(ct.Description, c.Description) AS Description
            FROM [catalog].[Courses] c
            LEFT JOIN [catalog].[CourseTranslations] ct ON ct.CourseId = c.Id AND ct.Locale = @Locale
            WHERE c.Slug = @Slug AND c.IsActive = 1
            """,
            new { Slug = slug, Locale = locale });
    }

    public async Task<IReadOnlyList<CourseSummaryDto>> GetCareerCoursesAsync(Guid careerId, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CourseSummaryDto>(
            """
            SELECT
                c.Id,
                c.Slug,
                COALESCE(ct.Name, c.Name) AS Name,
                c.ShortName,
                c.DegreeLevel,
                c.DurationYears
            FROM [catalog].[CareerCourses] cc
            JOIN [catalog].[Courses] c ON cc.CourseId = c.Id
            LEFT JOIN [catalog].[CourseTranslations] ct ON ct.CourseId = c.Id AND ct.Locale = @Locale
            WHERE cc.CareerId = @CareerId AND c.IsActive = 1
            ORDER BY cc.SortOrder, c.DegreeLevel, COALESCE(ct.Name, c.Name)
            """,
            new { CareerId = careerId, Locale = locale });
        return rows.ToList();
    }

    // ── Scholarships ──────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<ScholarshipDto> Items, int Total)> GetScholarshipsAsync(
        string locale, string? level, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var where = "WHERE s.IsActive = 1";
        var param = new DynamicParameters();
        param.Add("Locale", locale);
        param.Add("Offset", (page - 1) * pageSize);
        param.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(level))
        {
            where += " AND (s.Level = @Level OR s.Level = 'All')";
            param.Add("Level", level);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (COALESCE(st.Name, s.Name) LIKE @Search OR COALESCE(st.ProviderName, s.ProviderName) LIKE @Search OR COALESCE(st.EligibilitySummary, s.EligibilitySummary) LIKE @Search)";
            param.Add("Search", $"%{search}%");
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(1)
            FROM [catalog].[Scholarships] s
            LEFT JOIN [catalog].[ScholarshipTranslations] st ON st.ScholarshipId = s.Id AND st.Locale = @Locale
            {where}
            """, param);

        var items = await conn.QueryAsync<ScholarshipDto>(
            $"""
            SELECT
                s.Id,
                s.Slug,
                COALESCE(st.Name, s.Name) AS Name,
                COALESCE(st.ProviderName, s.ProviderName) AS ProviderName,
                s.Level,
                s.AmountLabel,
                COALESCE(st.EligibilitySummary, s.EligibilitySummary) AS EligibilitySummary,
                s.OfficialUrl,
                COALESCE(st.Disclaimer, s.Disclaimer) AS Disclaimer
            FROM [catalog].[Scholarships] s
            LEFT JOIN [catalog].[ScholarshipTranslations] st ON st.ScholarshipId = s.Id AND st.Locale = @Locale
            {where}
            ORDER BY COALESCE(st.Name, s.Name)
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, param);

        return (items.ToList(), total);
    }

    public async Task<ScholarshipDto?> GetScholarshipBySlugAsync(string slug, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ScholarshipDto>(
            """
            SELECT
                s.Id,
                s.Slug,
                COALESCE(st.Name, s.Name) AS Name,
                COALESCE(st.ProviderName, s.ProviderName) AS ProviderName,
                s.Level,
                s.AmountLabel,
                COALESCE(st.EligibilitySummary, s.EligibilitySummary) AS EligibilitySummary,
                s.OfficialUrl,
                COALESCE(st.Disclaimer, s.Disclaimer) AS Disclaimer
            FROM [catalog].[Scholarships] s
            LEFT JOIN [catalog].[ScholarshipTranslations] st ON st.ScholarshipId = s.Id AND st.Locale = @Locale
            WHERE s.Slug = @Slug AND s.IsActive = 1
            """,
            new { Slug = slug, Locale = locale });
    }

    // ── Enriched Career Detail ─────────────────────────────────────────────────

    public async Task<CareerDetailDto?> GetCareerDetailBySlugAsync(
        string slug, string locale, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        // First get the base career + translation (fallback to 'en' if locale not found)
        var career = await conn.QuerySingleOrDefaultAsync(
            """
            SELECT
                c.Id, c.Slug, c.CategoryId, c.IsFeatured,
                c.SalaryRangeLabel, c.MinEducationYears, c.MaxEducationYears,
                c.ImageUrl, c.PublishedAt,
                cat.Name AS CategoryName,
                COALESCE(t_loc.Title,   t_en.Title)       AS Title,
                COALESCE(t_loc.Summary, t_en.Summary)     AS Summary,
                COALESCE(t_loc.Description, t_en.Description) AS Description,
                COALESCE(t_loc.Disclaimer,  t_en.Disclaimer)  AS Disclaimer
            FROM [catalog].[Careers] c
            LEFT JOIN [catalog].[Categories] cat ON cat.Id = c.CategoryId
            LEFT JOIN [catalog].[CareerTranslations] t_en  ON t_en.CareerId  = c.Id AND t_en.Locale  = 'en'
            LEFT JOIN [catalog].[CareerTranslations] t_loc ON t_loc.CareerId = c.Id AND t_loc.Locale = @Locale
            WHERE c.Slug = @Slug AND c.Status = 'Published'
            """,
            new { Slug = slug, Locale = locale });

        if (career is null) return null;

        Guid careerId = career.Id;

        // Fan out 3 parallel detail queries
        var skillsTask   = GetCareerSkillsAsync(careerId, ct);
        var examsTask    = GetCareerExamsAsync(careerId, locale, ct);
        var coursesTask  = GetCareerCoursesAsync(careerId, locale, ct);
        await Task.WhenAll(skillsTask, examsTask, coursesTask);

        return new CareerDetailDto(
            Id:                 careerId,
            Slug:               career.Slug,
            Title:              career.Title ?? career.Slug,
            Summary:            career.Summary,
            Description:        career.Description,
            CategoryId:         career.CategoryId,
            CategoryName:       career.CategoryName,
            IsFeatured:         career.IsFeatured,
            SalaryRangeLabel:   career.SalaryRangeLabel,
            MinEducationYears:  career.MinEducationYears,
            MaxEducationYears:  career.MaxEducationYears,
            ImageUrl:           career.ImageUrl,
            Disclaimer:         career.Disclaimer,
            PublishedAt:        career.PublishedAt,
            Skills:             skillsTask.Result,
            Exams:              examsTask.Result,
            Courses:            coursesTask.Result);
    }
}
