using CareerPath.Application.Abstractions;

namespace CareerPath.Infrastructure.Auth;

/// <summary>
/// BCrypt password hasher. Work factor 12 is the recommended minimum for 2024+.
/// Never store plaintext — only hashes ever leave this class.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return BCrypt.Net.BCrypt.HashPassword(plainText, WorkFactor);
    }

    public bool Verify(string plainText, string hash)
    {
        if (string.IsNullOrWhiteSpace(plainText) || string.IsNullOrWhiteSpace(hash))
            return false;

        try { return BCrypt.Net.BCrypt.Verify(plainText, hash); }
        catch { return false; }
    }
}
