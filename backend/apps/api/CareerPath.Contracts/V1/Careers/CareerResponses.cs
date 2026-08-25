namespace CareerPath.Contracts.V1.Careers;

public sealed record CareerSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    string? CategoryId,
    string? ImageUrl,
    bool IsFeatured,
    string? SalaryRangeLabel,
    DateTimeOffset? PublishedAt
);

public sealed record CareerDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    string? Description,
    string? Disclaimer,
    string? CategoryId,
    string? ImageUrl,
    bool IsFeatured,
    string? SalaryRangeLabel,
    int MinEducationYears,
    int MaxEducationYears,
    string Locale,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt
);
