using MediatR;
using CareerPath.Contracts.V1.Careers;
using CareerPath.Contracts.V1.Common;
using CareerPath.Application.Abstractions.Repositories;

namespace CareerPath.Application.Careers;

public sealed record GetCareersQuery(
    string Locale,
    string? CategoryId,
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResponse<CareerSummaryResponse>>;

public sealed class GetCareersHandler : IRequestHandler<GetCareersQuery, PagedResponse<CareerSummaryResponse>>
{
    private readonly ICareerRepository _careerRepository;

    public GetCareersHandler(ICareerRepository careerRepository)
    {
        _careerRepository = careerRepository;
    }

    public async Task<PagedResponse<CareerSummaryResponse>> Handle(
        GetCareersQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new CareerPath.Contracts.V1.Common.PaginationRequest(request.Page, request.PageSize);

        var (items, totalCount) = await _careerRepository.GetPublishedAsync(
            locale: request.Locale,
            categoryId: request.CategoryId,
            searchTerm: request.Search,
            offset: pagination.Offset,
            pageSize: pagination.PageSize,
            cancellationToken: cancellationToken);

        var responses = items.Select(c => new CareerSummaryResponse(
            Id: c.Id,
            Slug: c.Slug,
            Title: c.Translation?.Title ?? c.Slug,
            Summary: c.Translation?.Summary,
            CategoryId: c.CategoryId,
            ImageUrl: c.ImageUrl,
            IsFeatured: c.IsFeatured,
            SalaryRangeLabel: c.SalaryRangeLabel,
            PublishedAt: c.PublishedAt
        )).ToList();

        return PagedResponse<CareerSummaryResponse>.Create(responses, pagination.Page, pagination.PageSize, totalCount);
    }
}
