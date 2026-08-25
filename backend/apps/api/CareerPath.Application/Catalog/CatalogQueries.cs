using MediatR;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Catalog;
using CareerPath.Contracts.V1.Common;

namespace CareerPath.Application.Catalog;

// ── Categories ────────────────────────────────────────────────────────────────

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICatalogRepository _repo;
    public GetCategoriesHandler(ICatalogRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery _, CancellationToken ct)
        => _repo.GetCategoriesAsync(ct);
}

// ── Skills ────────────────────────────────────────────────────────────────────

public sealed record GetSkillsQuery : IRequest<IReadOnlyList<SkillDto>>;

public sealed class GetSkillsHandler : IRequestHandler<GetSkillsQuery, IReadOnlyList<SkillDto>>
{
    private readonly ICatalogRepository _repo;
    public GetSkillsHandler(ICatalogRepository repo) => _repo = repo;

    public Task<IReadOnlyList<SkillDto>> Handle(GetSkillsQuery _, CancellationToken ct)
        => _repo.GetSkillsAsync(ct);
}

// ── Exams ─────────────────────────────────────────────────────────────────────

public sealed record GetExamsQuery(
    string? Level,
    string? Search,
    int Page,
    int PageSize,
    string Locale = "en") : IRequest<PagedResponse<ExamDto>>;

public sealed class GetExamsHandler : IRequestHandler<GetExamsQuery, PagedResponse<ExamDto>>
{
    private readonly ICatalogRepository _repo;
    public GetExamsHandler(ICatalogRepository repo) => _repo = repo;

    public async Task<PagedResponse<ExamDto>> Handle(GetExamsQuery q, CancellationToken ct)
    {
        var req = new PaginationRequest(q.Page, q.PageSize);
        var (items, total) = await _repo.GetExamsAsync(q.Locale, q.Level, q.Search, req.Page, req.PageSize, ct);
        return PagedResponse<ExamDto>.Create(items, req.Page, req.PageSize, total);
    }
}

public sealed record GetExamBySlugQuery(string Slug, string Locale = "en") : IRequest<ExamDto?>;

public sealed class GetExamBySlugHandler : IRequestHandler<GetExamBySlugQuery, ExamDto?>
{
    private readonly ICatalogRepository _repo;
    public GetExamBySlugHandler(ICatalogRepository repo) => _repo = repo;

    public Task<ExamDto?> Handle(GetExamBySlugQuery q, CancellationToken ct)
        => _repo.GetExamBySlugAsync(q.Slug, q.Locale, ct);
}

// ── Courses ───────────────────────────────────────────────────────────────────

public sealed record GetCoursesQuery(
    string? DegreeLevel,
    string? CategoryId,
    string? Search,
    int Page,
    int PageSize,
    string Locale = "en") : IRequest<PagedResponse<CourseDto>>;

public sealed class GetCoursesHandler : IRequestHandler<GetCoursesQuery, PagedResponse<CourseDto>>
{
    private readonly ICatalogRepository _repo;
    public GetCoursesHandler(ICatalogRepository repo) => _repo = repo;

    public async Task<PagedResponse<CourseDto>> Handle(GetCoursesQuery q, CancellationToken ct)
    {
        var req = new PaginationRequest(q.Page, q.PageSize);
        var (items, total) = await _repo.GetCoursesAsync(
            q.Locale, q.DegreeLevel, q.CategoryId, q.Search, req.Page, req.PageSize, ct);
        return PagedResponse<CourseDto>.Create(items, req.Page, req.PageSize, total);
    }
}

public sealed record GetCourseBySlugQuery(string Slug, string Locale = "en") : IRequest<CourseDto?>;

public sealed class GetCourseBySlugHandler : IRequestHandler<GetCourseBySlugQuery, CourseDto?>
{
    private readonly ICatalogRepository _repo;
    public GetCourseBySlugHandler(ICatalogRepository repo) => _repo = repo;

    public Task<CourseDto?> Handle(GetCourseBySlugQuery q, CancellationToken ct)
        => _repo.GetCourseBySlugAsync(q.Slug, q.Locale, ct);
}

// ── Scholarships ──────────────────────────────────────────────────────────────

public sealed record GetScholarshipsQuery(
    string? Level,
    string? Search,
    int Page,
    int PageSize,
    string Locale = "en") : IRequest<PagedResponse<ScholarshipDto>>;

public sealed class GetScholarshipsHandler : IRequestHandler<GetScholarshipsQuery, PagedResponse<ScholarshipDto>>
{
    private readonly ICatalogRepository _repo;
    public GetScholarshipsHandler(ICatalogRepository repo) => _repo = repo;

    public async Task<PagedResponse<ScholarshipDto>> Handle(GetScholarshipsQuery q, CancellationToken ct)
    {
        var req = new PaginationRequest(q.Page, q.PageSize);
        var (items, total) = await _repo.GetScholarshipsAsync(q.Locale, q.Level, q.Search, req.Page, req.PageSize, ct);
        return PagedResponse<ScholarshipDto>.Create(items, req.Page, req.PageSize, total);
    }
}

public sealed record GetScholarshipBySlugQuery(string Slug, string Locale = "en") : IRequest<ScholarshipDto?>;

public sealed class GetScholarshipBySlugHandler : IRequestHandler<GetScholarshipBySlugQuery, ScholarshipDto?>
{
    private readonly ICatalogRepository _repo;
    public GetScholarshipBySlugHandler(ICatalogRepository repo) => _repo = repo;

    public Task<ScholarshipDto?> Handle(GetScholarshipBySlugQuery q, CancellationToken ct)
        => _repo.GetScholarshipBySlugAsync(q.Slug, q.Locale, ct);
}

// ── Enriched Career Detail ────────────────────────────────────────────────────

public sealed record GetCareerDetailQuery(string Slug, string Locale = "en") : IRequest<CareerDetailDto?>;

public sealed class GetCareerDetailHandler : IRequestHandler<GetCareerDetailQuery, CareerDetailDto?>
{
    private readonly ICatalogRepository _repo;
    public GetCareerDetailHandler(ICatalogRepository repo) => _repo = repo;

    public Task<CareerDetailDto?> Handle(GetCareerDetailQuery q, CancellationToken ct)
        => _repo.GetCareerDetailBySlugAsync(q.Slug, q.Locale, ct);
}
