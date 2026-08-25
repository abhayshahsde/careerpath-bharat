namespace CareerPath.Domain.Entities;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    public static User Create(Guid id, string email, string passwordHash, string? displayName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = id,
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            DisplayName = displayName?.Trim(),
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void VerifyEmail(DateTimeOffset now)
    {
        IsEmailVerified = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void UpdatePassword(string newHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        PasswordHash = newHash;
        UpdatedAt = now;
    }
}
