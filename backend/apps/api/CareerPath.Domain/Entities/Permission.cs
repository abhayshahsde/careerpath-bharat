namespace CareerPath.Domain.Entities;

public sealed class Permission
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Module { get; init; } = string.Empty;
}
