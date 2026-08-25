namespace CareerPath.Domain.Entities;

public sealed record Career
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public string Status { get; init; } = CareerStatus.Draft;
    public bool IsFeatured { get; init; }
    public int MinEducationYears { get; init; }
    public int MaxEducationYears { get; init; }
    public string? SalaryRangeLabel { get; init; }
    public string? ImageUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }

    // Loaded separately via multi-map split in Dapper
    public CareerTranslation? Translation { get; init; }
}

public sealed record CareerTranslation
{
    public Guid CareerId { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public string? Disclaimer { get; init; }
}

public static class CareerStatus
{
    public const string Draft       = "Draft";
    public const string UnderReview = "UnderReview";
    public const string Published   = "Published";
    public const string Archived    = "Archived";
}
