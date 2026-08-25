namespace CareerPath.Contracts.V1.Student;

// ── Onboarding ────────────────────────────────────────────────────────────────

public sealed record OnboardingAnswerRequest(
    int Step,
    string QuestionKey,
    string Answer);

public sealed record SetCareerInterestsRequest(
    IReadOnlyList<string> CategoryIds);  // ordered by preference

public sealed record OnboardingStatusResponse(
    bool IsComplete,
    int StepsAnswered,
    int TotalSteps,
    IReadOnlyList<string> InterestedCategoryIds);

// ── Privacy / Consent ─────────────────────────────────────────────────────────

public sealed record ConsentRequest(
    string ConsentType,
    bool Granted);

public sealed record ConsentStatusResponse(
    string ConsentType,
    bool Granted,
    DateTimeOffset RecordedAt,
    string ConsentVersion);

public sealed record RequestDataDeletionRequest(
    string Reason);

public sealed record DataDeletionStatusResponse(
    int RequestId,
    DateTimeOffset RequestedAt,
    DateTimeOffset ScheduledFor,
    string Status);
