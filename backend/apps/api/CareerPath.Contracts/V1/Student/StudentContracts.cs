namespace CareerPath.Contracts.V1.Student;

public sealed record StudentProfileResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? CurrentEducationLevel,
    string? StateOfResidence,
    string? PreferredLocale,
    string? SchoolBoard,
    string? StreamOrSubjects,
    IReadOnlyList<string>? Interests,
    bool IsOnboardingComplete,
    DateTimeOffset UpdatedAt
);

public sealed record UpsertProfileRequest(
    string? DisplayName,
    string? CurrentEducationLevel,
    string? StateOfResidence,
    string? PreferredLocale,
    string? SchoolBoard,
    string? StreamOrSubjects,
    IReadOnlyList<string>? Interests
);

public sealed record SavedCareerResponse(
    Guid Id,
    Guid CareerId,
    string? CareerTitle,
    string? CareerSlug,
    DateTimeOffset SavedAt
);

public sealed record SavedCourseResponse(
    Guid Id,
    int CourseId,
    string? CourseName,
    string? CourseSlug,
    string? DegreeLevel,
    decimal DurationYears,
    DateTimeOffset SavedAt
);
