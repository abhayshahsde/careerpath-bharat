using System.Security.Claims;
using CareerPath.Application.Abstractions;

namespace CareerPath.Api.Identity;

/// <summary>
/// Reads the current user's identity from JWT claims and makes it available to use cases.
/// No business logic here — just claim extraction.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var claim = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
