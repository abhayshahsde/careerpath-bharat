using Dapper;
using CareerPath.Domain.Entities;
using CareerPath.Infrastructure.Data;
using CareerPath.Application.Abstractions.Repositories;

namespace CareerPath.Infrastructure.Repositories;

public sealed class CareerRepository : ICareerRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CareerRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IReadOnlyList<Career> Items, int TotalCount)> GetPublishedAsync(
        string locale,
        string? categoryId,
        string? searchTerm,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var where = new List<string> { "c.Status = 'Published'" };
        var param = new DynamicParameters();
        param.Add("Locale", locale);
        param.Add("Offset", offset);
        param.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            where.Add("c.CategoryId = @CategoryId");
            param.Add("CategoryId", categoryId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            where.Add("(ct.Title LIKE @Search OR ct.Summary LIKE @Search)");
            param.Add("Search", $"%{searchTerm}%");
        }

        var whereClause = string.Join(" AND ", where);

        var countSql = $"""
            SELECT COUNT(*)
            FROM [catalog].[Careers] c
            LEFT JOIN [catalog].[CareerTranslations] ct
                ON ct.CareerId = c.Id AND ct.Locale = @Locale
            WHERE {whereClause}
            """;

        var dataSql = $"""
            SELECT
                c.Id,
                c.Slug,
                c.CategoryId,
                c.Status,
                c.IsFeatured,
                c.MinEducationYears,
                c.MaxEducationYears,
                c.SalaryRangeLabel,
                c.ImageUrl,
                c.CreatedAt,
                c.UpdatedAt,
                c.PublishedAt,
                ct.CareerId,
                ct.Locale,
                ct.Title,
                ct.Summary
            FROM [catalog].[Careers] c
            LEFT JOIN [catalog].[CareerTranslations] ct
                ON ct.CareerId = c.Id AND ct.Locale = @Locale
            WHERE {whereClause}
            ORDER BY c.IsFeatured DESC, c.PublishedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);

        var careers = new List<Career>();
        await connection.QueryAsync<Career, CareerTranslation?, Career>(
            dataSql,
            (career, translation) =>
            {
                // Combine into a new record with translation attached
                var withTranslation = career with { Translation = translation };
                careers.Add(withTranslation);
                return withTranslation;
            },
            param,
            splitOn: "CareerId");

        return (careers, totalCount);
    }

    public async Task<Career?> GetBySlugAsync(
        string slug,
        string locale,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        Career? result = null;

        await connection.QueryAsync<Career, CareerTranslation?, Career>(
            """
            SELECT
                c.Id,
                c.Slug,
                c.CategoryId,
                c.Status,
                c.IsFeatured,
                c.MinEducationYears,
                c.MaxEducationYears,
                c.SalaryRangeLabel,
                c.ImageUrl,
                c.CreatedAt,
                c.UpdatedAt,
                c.PublishedAt,
                ct.CareerId,
                ct.Locale,
                ct.Title,
                ct.Summary,
                ct.Description,
                ct.Disclaimer
            FROM [catalog].[Careers] c
            LEFT JOIN [catalog].[CareerTranslations] ct
                ON ct.CareerId = c.Id AND ct.Locale = @Locale
            WHERE c.Slug = @Slug
              AND c.Status = 'Published'
            """,
            (career, translation) =>
            {
                result = career with { Translation = translation };
                return result;
            },
            new { Slug = slug, Locale = locale },
            splitOn: "CareerId");

        return result;
    }
}
