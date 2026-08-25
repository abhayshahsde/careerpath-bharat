namespace CareerPath.Domain.Entities;

public sealed class StudentProfile
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CurrentEducationLevel { get; private set; }
    public string? StateOfResidence { get; private set; }
    public string? PreferredLocale { get; private set; }
    public string? SchoolBoard { get; private set; }
    public string? StreamOrSubjects { get; private set; }
    public IReadOnlyList<string>? Interests { get; private set; }
    public bool IsOnboardingComplete { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private StudentProfile() { }

    public static StudentProfile Create(Guid userId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsOnboardingComplete = false,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string? displayName,
        string? currentEducationLevel,
        string? stateOfResidence,
        string? preferredLocale,
        string? schoolBoard,
        string? streamOrSubjects,
        IReadOnlyList<string>? interests,
        DateTimeOffset now)
    {
        DisplayName = displayName?.Trim();
        CurrentEducationLevel = currentEducationLevel;
        StateOfResidence = stateOfResidence;
        PreferredLocale = preferredLocale;
        SchoolBoard = schoolBoard;
        StreamOrSubjects = streamOrSubjects;
        Interests = interests;
        UpdatedAt = now;
    }

    public void CompleteOnboarding(DateTimeOffset now)
    {
        IsOnboardingComplete = true;
        UpdatedAt = now;
    }
}

public sealed class SavedItem
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string ItemType { get; init; } = string.Empty;
    public Guid ItemId { get; init; }
    public DateTimeOffset SavedAt { get; init; }
}

public static class SavedItemType
{
    public const string Career = "Career";
    public const string Course = "Course";
    public const string Scholarship = "Scholarship";
}
