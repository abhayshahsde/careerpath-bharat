using CareerPath.Domain.Entities;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task RecordLoginAttemptAsync(string email, string? ipAddress, bool succeeded, CancellationToken cancellationToken = default);
    Task<int> CountRecentFailedAttemptsAsync(string email, TimeSpan window, CancellationToken cancellationToken = default);
}

public interface ISessionRepository
{
    Task<Guid> CreateSessionAsync(Guid userId, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task CreateRefreshTokenAsync(Guid sessionId, string tokenHash, Guid familyId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task MarkRefreshTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken = default);
    Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Session?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
