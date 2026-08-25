using CareerPath.Contracts.V1.Catalog;

namespace CareerPath.Application.Abstractions.Repositories;

public interface ICatalogRepository
{
    // Categories
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);

    // Skills
    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillDto>> GetCareerSkillsAsync(Guid careerId, CancellationToken ct = default);

    // Exams
    Task<(IReadOnlyList<ExamDto> Items, int Total)> GetExamsAsync(
        string locale, string? level, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ExamDto?> GetExamBySlugAsync(string slug, string locale, CancellationToken ct = default);
    Task<IReadOnlyList<ExamSummaryDto>> GetCareerExamsAsync(Guid careerId, string locale, CancellationToken ct = default);

    // Courses
    Task<(IReadOnlyList<CourseDto> Items, int Total)> GetCoursesAsync(
        string locale, string? degreeLevel, string? categoryId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<CourseDto?> GetCourseBySlugAsync(string slug, string locale, CancellationToken ct = default);
    Task<IReadOnlyList<CourseSummaryDto>> GetCareerCoursesAsync(Guid careerId, string locale, CancellationToken ct = default);

    // Scholarships
    Task<(IReadOnlyList<ScholarshipDto> Items, int Total)> GetScholarshipsAsync(
        string locale, string? level, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ScholarshipDto?> GetScholarshipBySlugAsync(string slug, string locale, CancellationToken ct = default);

    // Enriched career detail
    Task<CareerDetailDto?> GetCareerDetailBySlugAsync(string slug, string locale, CancellationToken ct = default);
}
