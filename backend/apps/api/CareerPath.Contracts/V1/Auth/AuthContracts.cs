namespace CareerPath.Contracts.V1.Auth;

// ── Register ─────────────────────────────────────────────────────────────────

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName);

// ── Login ─────────────────────────────────────────────────────────────────────

public sealed record LoginRequest(
    string Email,
    string Password,
    string? DeviceInfo);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles);

// ── Refresh ───────────────────────────────────────────────────────────────────

public sealed record RefreshRequest(string RefreshToken);

// ── Verify Email ──────────────────────────────────────────────────────────────

public sealed record VerifyEmailRequest(string Token);

// ── Change Password ───────────────────────────────────────────────────────────

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

// ── Forgot / Reset Password ───────────────────────────────────────────────────

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);
