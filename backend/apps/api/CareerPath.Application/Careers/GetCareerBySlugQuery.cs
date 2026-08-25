using MediatR;
using CareerPath.Contracts.V1.Careers;
using CareerPath.Application.Abstractions.Repositories;

namespace CareerPath.Application.Careers;

public sealed record GetCareerBySlugQuery(string Slug, string Locale) : IRequest<CareerDetailResponse?>;

public sealed class GetCareerBySlugHandler : IRequestHandler<GetCareerBySlugQuery, CareerDetailResponse?>
{
    private readonly ICareerRepository _careerRepository;

    public GetCareerBySlugHandler(ICareerRepository careerRepository)
    {
        _careerRepository = careerRepository;
    }

    public async Task<CareerDetailResponse?> Handle(
        GetCareerBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var career = await _careerRepository.GetBySlugAsync(request.Slug, request.Locale, cancellationToken);

        if (career is null) return null;

        return new CareerDetailResponse(
            Id: career.Id,
            Slug: career.Slug,
            Title: career.Translation?.Title ?? career.Slug,
            Summary: career.Translation?.Summary,
            Description: career.Translation?.Description,
            Disclaimer: career.Translation?.Disclaimer,
            CategoryId: career.CategoryId,
            ImageUrl: career.ImageUrl,
            IsFeatured: career.IsFeatured,
            SalaryRangeLabel: career.SalaryRangeLabel,
            MinEducationYears: career.MinEducationYears,
            MaxEducationYears: career.MaxEducationYears,
            Locale: request.Locale,
            PublishedAt: career.PublishedAt,
            UpdatedAt: career.UpdatedAt
        );
    }
}
