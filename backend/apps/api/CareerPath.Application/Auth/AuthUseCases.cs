using MediatR;
using FluentValidation;
using CareerPath.Contracts.V1.Auth;
using CareerPath.Application.Abstractions;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Application.Abstractions.Services;
using CareerPath.Domain.Entities;

namespace CareerPath.Application.Auth;

// ── Register ──────────────────────────────────────────────────────────────────

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName,
    string? IpAddress) : IRequest<AuthResponse>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => x.DisplayName is not null);
    }
}

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterHandler(
        IUserRepository users,
        ISessionRepository sessions,
        IPasswordHasher hasher,
        ITokenService tokens)
    {
        _users    = users;
        _sessions = sessions;
        _hasher   = hasher;
        _tokens   = tokens;
    }

    public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await _users.ExistsByEmailAsync(cmd.Email, ct))
            throw new InvalidOperationException($"An account with email '{cmd.Email}' already exists.");

        var now  = DateTimeOffset.UtcNow;
        var user = User.Create(Guid.NewGuid(), cmd.Email, _hasher.Hash(cmd.Password), cmd.DisplayName, now);

        await _users.CreateAsync(user, ct);
        await _users.AssignRoleAsync(user.Id, "Student", ct);

        return await IssueTokensAsync(user, deviceInfo: null, cmd.IpAddress, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, string? deviceInfo, string? ipAddress, CancellationToken ct)
    {
        var roles      = await _users.GetRolesAsync(user.Id, ct);
        var sessionId  = await _sessions.CreateSessionAsync(user.Id, deviceInfo, ipAddress, ct);
        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email, roles);
        var (rawRefresh, refreshHash) = _tokens.GenerateRefreshToken();
        var familyId = Guid.NewGuid();

        await _sessions.CreateRefreshTokenAsync(
            sessionId, refreshHash, familyId, _tokens.RefreshTokenExpiresAt(), ct);

        return new AuthResponse(
            AccessToken:            accessToken,
            RefreshToken:           rawRefresh,
            AccessTokenExpiresAt:   _tokens.AccessTokenExpiresAt(),
            RefreshTokenExpiresAt:  _tokens.RefreshTokenExpiresAt(),
            User: new UserSummary(user.Id, user.Email, user.DisplayName, user.IsEmailVerified, roles));
    }
}

// ── Login ─────────────────────────────────────────────────────────────────────

public sealed record LoginCommand(
    string Email,
    string Password,
    string? DeviceInfo,
    string? IpAddress) : IRequest<AuthResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(
        IUserRepository users,
        ISessionRepository sessions,
        IPasswordHasher hasher,
        ITokenService tokens)
    {
        _users    = users;
        _sessions = sessions;
        _hasher   = hasher;
        _tokens   = tokens;
    }

    public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
    {
        // Brute-force check — max 10 failed attempts per 15 minutes
        var recentFails = await _users.CountRecentFailedAttemptsAsync(
            cmd.Email, TimeSpan.FromMinutes(15), ct);

        if (recentFails >= 10)
            throw new InvalidOperationException("Too many failed login attempts. Try again in 15 minutes.");

        var user = await _users.FindByEmailAsync(cmd.Email, ct);
        var succeeded = user is not null && user.IsActive && _hasher.Verify(cmd.Password, user.PasswordHash);

        await _users.RecordLoginAttemptAsync(cmd.Email, cmd.IpAddress, succeeded, ct);

        if (!succeeded)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles      = await _users.GetRolesAsync(user!.Id, ct);
        var sessionId  = await _sessions.CreateSessionAsync(user.Id, cmd.DeviceInfo, cmd.IpAddress, ct);
        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email, roles);
        var (rawRefresh, refreshHash) = _tokens.GenerateRefreshToken();
        var familyId = Guid.NewGuid();

        await _sessions.CreateRefreshTokenAsync(
            sessionId, refreshHash, familyId, _tokens.RefreshTokenExpiresAt(), ct);

        return new AuthResponse(
            AccessToken:            accessToken,
            RefreshToken:           rawRefresh,
            AccessTokenExpiresAt:   _tokens.AccessTokenExpiresAt(),
            RefreshTokenExpiresAt:  _tokens.RefreshTokenExpiresAt(),
            User: new UserSummary(user.Id, user.Email, user.DisplayName, user.IsEmailVerified, roles));
    }
}

// ── Refresh Token ─────────────────────────────────────────────────────────────

public sealed record RefreshTokenCommand(string RawRefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly ITokenService _tokens;

    public RefreshTokenHandler(
        IUserRepository users,
        ISessionRepository sessions,
        ITokenService tokens)
    {
        _users    = users;
        _sessions = sessions;
        _tokens   = tokens;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        // Hash the incoming raw token to look it up
        var incomingHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(cmd.RawRefreshToken)));

        var token = await _sessions.FindRefreshTokenByHashAsync(incomingHash, ct);

        if (token is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        // Reuse detection — if already used, revoke entire family (token theft)
        if (token.IsUsed)
        {
            await _sessions.RevokeTokenFamilyAsync(token.FamilyId, ct);
            throw new UnauthorizedAccessException("Refresh token already used. All sessions revoked.");
        }

        if (token.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        var session = await _sessions.FindSessionAsync(token.SessionId, ct);
        if (session is null || !session.IsActive)
            throw new UnauthorizedAccessException("Session has been revoked.");

        var user = await _users.FindByIdAsync(session.UserId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Account not found or disabled.");

        // Mark old token as used
        await _sessions.MarkRefreshTokenUsedAsync(token.Id, ct);

        // Issue a new refresh token in the same family (rotation)
        var roles = await _users.GetRolesAsync(user.Id, ct);
        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email, roles);
        var (newRawToken, newHash) = _tokens.GenerateRefreshToken();

        await _sessions.CreateRefreshTokenAsync(
            token.SessionId, newHash, token.FamilyId, _tokens.RefreshTokenExpiresAt(), ct);

        return new AuthResponse(
            AccessToken:            accessToken,
            RefreshToken:           newRawToken,
            AccessTokenExpiresAt:   _tokens.AccessTokenExpiresAt(),
            RefreshTokenExpiresAt:  _tokens.RefreshTokenExpiresAt(),
            User: new UserSummary(user.Id, user.Email, user.DisplayName, user.IsEmailVerified, roles));
    }
}

// ── Logout ────────────────────────────────────────────────────────────────────

public sealed record LogoutCommand(Guid UserId) : IRequest<Unit>;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly ISessionRepository _sessions;
    public LogoutHandler(ISessionRepository sessions) => _sessions = sessions;

    public async Task<Unit> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        await _sessions.RevokeAllUserSessionsAsync(cmd.UserId, ct);
        return Unit.Value;
    }
}

// ── Change Password ───────────────────────────────────────────────────────────

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current.");
    }
}

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IPasswordHasher _hasher;

    public ChangePasswordHandler(
        IUserRepository users, ISessionRepository sessions, IPasswordHasher hasher)
    {
        _users    = users;
        _sessions = sessions;
        _hasher   = hasher;
    }

    public async Task<Unit> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!_hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.UpdatePassword(_hasher.Hash(cmd.NewPassword), DateTimeOffset.UtcNow);
        await _users.UpdateAsync(user, ct);

        // Revoke all sessions — forces re-login on all devices
        await _sessions.RevokeAllUserSessionsAsync(cmd.UserId, ct);
        return Unit.Value;
    }
}

// ── Forgot Password Send OTP ──────────────────────────────────────────────────

public sealed record SendPasswordResetOtpCommand(
    string Identifier,
    string Channel) : IRequest<SendPasswordOtpResponse>;

public sealed class SendPasswordResetOtpValidator : AbstractValidator<SendPasswordResetOtpCommand>
{
    public SendPasswordResetOtpValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().WithMessage("Email address or WhatsApp phone number is required.");
        RuleFor(x => x.Channel).Must(c => c == "Email" || c == "WhatsApp")
            .WithMessage("Channel must be either 'Email' or 'WhatsApp'.");
    }
}

public sealed class SendPasswordResetOtpHandler : IRequestHandler<SendPasswordResetOtpCommand, SendPasswordOtpResponse>
{
    private readonly IUserRepository _users;
    private readonly IOtpRepository _otpRepo;
    private readonly IOtpDeliveryService _otpDelivery;

    public SendPasswordResetOtpHandler(
        IUserRepository users,
        IOtpRepository otpRepo,
        IOtpDeliveryService otpDelivery)
    {
        _users = users;
        _otpRepo = otpRepo;
        _otpDelivery = otpDelivery;
    }

    public async Task<SendPasswordOtpResponse> Handle(SendPasswordResetOtpCommand cmd, CancellationToken ct)
    {
        var cleanId = cmd.Identifier.Trim();
        var isEmail = cmd.Channel == "Email" || cleanId.Contains('@');
        var channel = isEmail ? "Email" : "WhatsApp";

        string displayName = "Student";
        if (isEmail)
        {
            var user = await _users.FindByEmailAsync(cleanId, ct);
            if (user is null)
            {
                // Return success message to avoid account enumeration, but don't dispatch
                return new SendPasswordOtpResponse(
                    true,
                    $"If an account exists with {cleanId}, an OTP verification code has been dispatched via {channel}.",
                    60,
                    600);
            }
            displayName = user.DisplayName ?? "Student";
        }

        // Generate 6-digit numeric OTP
        var otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        await _otpRepo.CreateOtpAsync(cleanId, otpCode, channel, expiresAt, ct);

        if (channel == "WhatsApp")
        {
            await _otpDelivery.SendWhatsAppOtpAsync(cleanId, otpCode, displayName, ct);
        }
        else
        {
            await _otpDelivery.SendEmailOtpAsync(cleanId, otpCode, displayName, ct);
        }

        return new SendPasswordOtpResponse(
            true,
            $"A 6-digit verification code has been sent to your registered {channel} ({cleanId}). Valid for 10 minutes.",
            60,
            600);
    }
}

// ── Verify Password Reset OTP ─────────────────────────────────────────────────

public sealed record VerifyPasswordResetOtpCommand(
    string Identifier,
    string Otp) : IRequest<VerifyPasswordOtpResponse>;

public sealed class VerifyPasswordResetOtpValidator : AbstractValidator<VerifyPasswordResetOtpCommand>
{
    public VerifyPasswordResetOtpValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().WithMessage("Identifier is required.");
        RuleFor(x => x.Otp).NotEmpty().Length(6).WithMessage("A 6-digit OTP is required.");
    }
}

public sealed class VerifyPasswordResetOtpHandler : IRequestHandler<VerifyPasswordResetOtpCommand, VerifyPasswordOtpResponse>
{
    private readonly IOtpRepository _otpRepo;

    public VerifyPasswordResetOtpHandler(IOtpRepository otpRepo)
    {
        _otpRepo = otpRepo;
    }

    public async Task<VerifyPasswordOtpResponse> Handle(VerifyPasswordResetOtpCommand cmd, CancellationToken ct)
    {
        var record = await _otpRepo.GetActiveOtpAsync(cmd.Identifier.Trim(), ct);
        if (record is null)
        {
            throw new InvalidOperationException("The OTP code has expired or is invalid. Please request a new OTP.");
        }

        if (record.AttemptCount >= 5)
        {
            throw new InvalidOperationException("Maximum verification attempts exceeded. Please request a new OTP code.");
        }

        if (record.OtpCode != cmd.Otp.Trim())
        {
            await _otpRepo.IncrementAttemptsAsync(record.Id, ct);
            var remaining = Math.Max(0, 4 - record.AttemptCount);
            throw new InvalidOperationException($"Incorrect verification code. Attempts remaining: {remaining}.");
        }

        var resetToken = await _otpRepo.MarkOtpVerifiedAndGenerateResetTokenAsync(record.Id, ct);

        return new VerifyPasswordOtpResponse(
            true,
            resetToken,
            "OTP verified successfully! Please enter your new password below.");
    }
}

// ── Reset Password With Verified Token ────────────────────────────────────────

public sealed record ResetPasswordWithTokenCommand(
    string ResetToken,
    string NewPassword) : IRequest<Unit>;

public sealed class ResetPasswordWithTokenValidator : AbstractValidator<ResetPasswordWithTokenCommand>
{
    public ResetPasswordWithTokenValidator()
    {
        RuleFor(x => x.ResetToken).NotEmpty().WithMessage("Valid reset token is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public sealed class ResetPasswordWithTokenHandler : IRequestHandler<ResetPasswordWithTokenCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IOtpRepository _otpRepo;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordWithTokenHandler(
        IUserRepository users,
        ISessionRepository sessions,
        IOtpRepository otpRepo,
        IPasswordHasher hasher)
    {
        _users = users;
        _sessions = sessions;
        _otpRepo = otpRepo;
        _hasher = hasher;
    }

    public async Task<Unit> Handle(ResetPasswordWithTokenCommand cmd, CancellationToken ct)
    {
        var record = await _otpRepo.GetByResetTokenAsync(cmd.ResetToken.Trim(), ct);
        if (record is null)
        {
            throw new InvalidOperationException("Password reset authorization token is invalid or has expired. Please request a new OTP.");
        }

        User? user = null;
        if (record.Identifier.Contains('@'))
        {
            user = await _users.FindByEmailAsync(record.Identifier, ct);
        }

        if (user is null)
        {
            throw new InvalidOperationException("User account associated with this verification was not found.");
        }

        user.UpdatePassword(_hasher.Hash(cmd.NewPassword), DateTimeOffset.UtcNow);
        await _users.UpdateAsync(user, ct);

        // Revoke all prior sessions for security
        await _sessions.RevokeAllUserSessionsAsync(user.Id, ct);

        // Invalidate the reset token
        await _otpRepo.InvalidateResetTokenAsync(record.Id, ct);

        return Unit.Value;
    }
}
