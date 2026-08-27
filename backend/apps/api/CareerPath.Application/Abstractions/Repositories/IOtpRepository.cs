namespace CareerPath.Application.Abstractions.Repositories;

public sealed record OtpRecord(
    Guid Id,
    string Identifier,
    string OtpCode,
    string Channel,
    string? ResetToken,
    DateTimeOffset ExpiresAt,
    int AttemptCount,
    bool IsUsed,
    DateTimeOffset CreatedAt);

public interface IOtpRepository
{
    Task CreateOtpAsync(string identifier, string otpCode, string channel, DateTimeOffset expiresAt, CancellationToken ct = default);
    Task<OtpRecord?> GetActiveOtpAsync(string identifier, CancellationToken ct = default);
    Task IncrementAttemptsAsync(Guid otpId, CancellationToken ct = default);
    Task<string> MarkOtpVerifiedAndGenerateResetTokenAsync(Guid otpId, CancellationToken ct = default);
    Task<OtpRecord?> GetByResetTokenAsync(string resetToken, CancellationToken ct = default);
    Task InvalidateResetTokenAsync(Guid otpId, CancellationToken ct = default);
}
