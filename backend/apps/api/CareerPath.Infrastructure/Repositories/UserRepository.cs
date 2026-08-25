using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Domain.Entities;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _db;
    public UserRepository(ISqlConnectionFactory db) => _db = db;

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<User>(
            """
            SELECT Id, Email, PasswordHash, DisplayName, IsEmailVerified, IsActive, CreatedAt, UpdatedAt
            FROM [identity].[Users]
            WHERE Email = @Email AND IsActive = 1
            """,
            new { Email = email.ToLowerInvariant().Trim() });
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<User>(
            """
            SELECT Id, Email, PasswordHash, DisplayName, IsEmailVerified, IsActive, CreatedAt, UpdatedAt
            FROM [identity].[Users]
            WHERE Id = @Id
            """,
            new { Id = id });
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT CAST(COUNT(1) AS BIT) FROM [identity].[Users] WHERE Email = @Email",
            new { Email = email.ToLowerInvariant().Trim() });
    }

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[Users] (Id, Email, PasswordHash, DisplayName, IsEmailVerified, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Email, @PasswordHash, @DisplayName, @IsEmailVerified, @IsActive, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.DisplayName,
                user.IsEmailVerified,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            });
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [identity].[Users]
            SET PasswordHash = @PasswordHash,
                DisplayName  = @DisplayName,
                IsEmailVerified = @IsEmailVerified,
                IsActive     = @IsActive,
                UpdatedAt    = @UpdatedAt
            WHERE Id = @Id
            """,
            new
            {
                user.Id,
                user.PasswordHash,
                user.DisplayName,
                user.IsEmailVerified,
                user.IsActive,
                user.UpdatedAt
            });
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var roles = await conn.QueryAsync<string>(
            """
            SELECT r.Name
            FROM [identity].[UserRoles] ur
            JOIN [identity].[Roles] r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId AND r.IsActive = 1
            """,
            new { UserId = userId });
        return roles.ToList();
    }

    public async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[UserRoles] (UserId, RoleId)
            SELECT @UserId, Id FROM [identity].[Roles] WHERE Name = @RoleName
            """,
            new { UserId = userId, RoleName = roleName });
    }

    public async Task RecordLoginAttemptAsync(
        string email, string? ipAddress, bool succeeded, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[LoginAttempts] (Email, IpAddress, Succeeded, AttemptedAt)
            VALUES (@Email, @IpAddress, @Succeeded, SYSUTCDATETIME())
            """,
            new { Email = email.ToLowerInvariant().Trim(), IpAddress = ipAddress, Succeeded = succeeded });
    }

    public async Task<int> CountRecentFailedAttemptsAsync(
        string email, TimeSpan window, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        var since = DateTimeOffset.UtcNow.Subtract(window);
        return await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1) FROM [identity].[LoginAttempts]
            WHERE Email = @Email AND Succeeded = 0 AND AttemptedAt >= @Since
            """,
            new { Email = email.ToLowerInvariant().Trim(), Since = since });
    }
}
