using CareerPath.Domain.Entities;
using CareerPath.Contracts.V1.Student;

namespace CareerPath.Application.Abstractions.Repositories;

/// <summary>
/// Student profile repository interface — defined in Application, implemented in Infrastructure.
/// </summary>
public interface IStudentProfileRepository
{
    Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpsertAsync(
        Guid userId,
        string? displayName,
        string? educationLevel,
        string? state,
        string? locale,
        string? schoolBoard,
        string? streamOrSubjects,
        IReadOnlyList<string>? interests,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedItem>> GetSavedItemsAsync(Guid userId, string itemType, CancellationToken cancellationToken = default);
    Task<bool> SaveItemAsync(Guid userId, string itemType, Guid itemId, CancellationToken cancellationToken = default);
    Task<bool> UnsaveItemAsync(Guid userId, string itemType, Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedCareerResponse>> GetSavedCareersWithDetailsAsync(Guid userId, string locale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedCourseResponse>> GetSavedCoursesWithDetailsAsync(Guid userId, string locale, CancellationToken cancellationToken = default);
}
