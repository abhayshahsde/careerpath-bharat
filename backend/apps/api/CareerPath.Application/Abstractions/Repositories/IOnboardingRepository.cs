using CareerPath.Contracts.V1.Student;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IOnboardingRepository
{
    Task SaveAnswerAsync(Guid userId, int step, string questionKey, string answer, CancellationToken ct = default);
    Task<IReadOnlyList<(string QuestionKey, string Answer)>> GetAnswersAsync(Guid userId, CancellationToken ct = default);

    Task SetCareerInterestsAsync(Guid userId, IReadOnlyList<string> categoryIds, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCareerInterestsAsync(Guid userId, CancellationToken ct = default);

    Task<OnboardingStatusResponse> GetStatusAsync(Guid userId, CancellationToken ct = default);
}

public interface IPrivacyRepository
{
    Task RecordConsentAsync(Guid userId, string consentType, bool granted,
        string? ipAddress, string? userAgent, CancellationToken ct = default);
    Task<IReadOnlyList<ConsentStatusResponse>> GetConsentsAsync(Guid userId, CancellationToken ct = default);

    Task<DataDeletionStatusResponse> RequestDataDeletionAsync(Guid userId, string reason, CancellationToken ct = default);
    Task<DataDeletionStatusResponse?> GetDeletionRequestAsync(Guid userId, CancellationToken ct = default);
    Task CancelDeletionRequestAsync(Guid userId, CancellationToken ct = default);
}
