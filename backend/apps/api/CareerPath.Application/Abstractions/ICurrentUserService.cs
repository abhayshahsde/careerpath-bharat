namespace CareerPath.Application.Abstractions;

/// <summary>
/// Provides the current authenticated user's identity and permissions to use cases.
/// Populated from JWT claims by the API layer.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
