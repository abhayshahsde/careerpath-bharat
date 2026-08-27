using CareerPath.Application.Auth;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create a new student account")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticate and receive tokens")
            .AllowAnonymous();

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithSummary("Rotate access + refresh token pair")
            .AllowAnonymous();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Revoke all sessions for the current user")
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword")
            .WithSummary("Change the authenticated user's password")
            .RequireAuthorization();

        // ── Forgot Password & OTP ─────────────────────────────────────────────
        group.MapPost("/forgot-password/send-otp", SendPasswordOtp)
            .WithName("SendPasswordResetOtp")
            .WithSummary("Send 6-digit OTP code to registered Email (Gmail) or WhatsApp")
            .AllowAnonymous();

        group.MapPost("/forgot-password/verify-otp", VerifyPasswordOtp)
            .WithName("VerifyPasswordResetOtp")
            .WithSummary("Verify 6-digit OTP code and obtain password reset authorization token")
            .AllowAnonymous();

        group.MapPost("/forgot-password/reset", ResetPasswordWithOtp)
            .WithName("ResetPasswordWithOtp")
            .WithSummary("Reset account password using verified OTP reset token")
            .AllowAnonymous();
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest req,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var cmd = new RegisterCommand(
            req.Email,
            req.Password,
            req.DisplayName,
            IpAddress: ctx.Connection.RemoteIpAddress?.ToString());

        var result = await mediator.Send(cmd, ct);
        return Results.Created("/api/v1/me/profile", result);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest req,
        IMediator mediator,
        HttpContext ctx,
        CancellationToken ct)
    {
        var cmd = new LoginCommand(
            req.Email,
            req.Password,
            req.DeviceInfo,
            IpAddress: ctx.Connection.RemoteIpAddress?.ToString());

        var result = await mediator.Send(cmd, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Refresh(
        [FromBody] RefreshRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshTokenCommand(req.RefreshToken), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> Logout(
        ICurrentUserService currentUser,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(currentUser.UserId!.Value), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePassword(
        [FromBody] ChangePasswordRequest req,
        ICurrentUserService currentUser,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new ChangePasswordCommand(
            currentUser.UserId!.Value, req.CurrentPassword, req.NewPassword), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SendPasswordOtp(
        [FromBody] SendPasswordOtpRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new SendPasswordResetOtpCommand(req.Identifier, req.Channel), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> VerifyPasswordOtp(
        [FromBody] VerifyPasswordOtpRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyPasswordResetOtpCommand(req.Identifier, req.Otp), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ResetPasswordWithOtp(
        [FromBody] ResetPasswordWithTokenRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordWithTokenCommand(req.ResetToken, req.NewPassword), ct);
        return Results.Ok(new { success = true, message = "Your password has been successfully reset. You can now log in." });
    }
}
