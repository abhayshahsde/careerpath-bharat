using CareerPath.Domain.Entities;

namespace CareerPath.Application.Abstractions.Repositories;

/// <summary>
/// Career repository interface — defined in Application, implemented in Infrastructure.
/// Application layer must never reference Infrastructure directly.
/// </summary>
public interface ICareerRepository
{
    Task<(IReadOnlyList<Career> Items, int TotalCount)> GetPublishedAsync(
        string locale,
        string? categoryId,
        string? searchTerm,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Career?> GetBySlugAsync(string slug, string locale, CancellationToken cancellationToken = default);
}
