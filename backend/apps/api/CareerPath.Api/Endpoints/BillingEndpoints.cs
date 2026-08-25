using MediatR;
using CareerPath.Application.Billing;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Billing;

namespace CareerPath.Api.Endpoints;

public static class BillingEndpoints
{
    public static void MapBilling(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/billing")
            .WithTags("Billing & Subscriptions")
            .RequireAuthorization();

        // 1. Get all active plans
        group.MapGet("/plans", ListPlans)
            .WithName("ListSubscriptionPlans")
            .WithSummary("List all active subscription plans and benefits")
            .AllowAnonymous(); // Permitted for public comparison pages

        // 2. Get public coupons
        group.MapGet("/coupons/public", ListPublicCoupons)
            .WithName("ListPublicCoupons")
            .WithSummary("List currently visible discount coupons")
            .AllowAnonymous();

        // 3. Validate coupon code
        group.MapPost("/coupons/validate", ValidateCoupon)
            .WithName("ValidateCoupon")
            .WithSummary("Validate promo code for plan and calculate discount");

        // 4. Get current active subscription details
        group.MapGet("/my-subscription", GetActiveSub)
            .WithName("GetMySubscription")
            .WithSummary("Get active subscription and premium status for current user");

        // 5. Initiate payment subscribe transaction checkout
        group.MapPost("/subscribe", Subscribe)
            .WithName("SubscribeToPlan")
            .WithSummary("Purchase premium features tier subscription plan");

        // 6. Cancel auto-renew of active subscription
        group.MapPost("/cancel", CancelSubscription)
            .WithName("CancelSubscriptionRenewal")
            .WithSummary("Cancel active subscription renewal at period end");

        // ── Admin Super Dashboard Endpoints (Admin role only) ─────────────
        var adminGroup = app.MapGroup("/api/v1/admin")
            .WithTags("Admin Super Control")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        adminGroup.MapGet("/overview", GetAdminOverview)
            .WithName("GetAdminOverview")
            .WithSummary("Get platform metrics, users count, subscriptions, and MRR");

        adminGroup.MapGet("/users", ListAdminUsers)
            .WithName("ListAdminUsers")
            .WithSummary("List and search all registered platform students and users");

        adminGroup.MapPatch("/users/{id:guid}/suspension", ToggleUserSuspension)
            .WithName("ToggleUserSuspension")
            .WithSummary("Suspend or reactivate user account");

        adminGroup.MapGet("/coupons", ListAllCoupons)
            .WithName("ListAllCoupons")
            .WithSummary("List all promotional coupons");

        adminGroup.MapPost("/coupons", CreateCoupon)
            .WithName("CreateCoupon")
            .WithSummary("Create new discount coupon");

        adminGroup.MapPatch("/coupons/{id:guid}/toggle", ToggleCoupon)
            .WithName("ToggleCoupon")
            .WithSummary("Toggle coupon active or public visibility status");
    }

    private static async Task<IResult> ListPlans(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListPlansQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListPublicCoupons(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListPublicCouponsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ValidateCoupon(
        ValidateCouponRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var result = await mediator.Send(new ValidateCouponQuery(userId, req.Code, req.PlanId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetActiveSub(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var result = await mediator.Send(new GetActiveSubscriptionQuery(userId), ct);
        return result is null ? Results.NotFound(new { message = "User does not have an active premium subscription." }) : Results.Ok(result);
    }

    private static async Task<IResult> Subscribe(
        CheckoutSessionRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        try
        {
            var userId = currentUser.UserId.GetValueOrDefault();
            var result = await mediator.Send(new SubscribeCommand(userId, req.PlanId, req.PaymentProvider, req.CardToken, req.CouponCode), ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CancelSubscription(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var activeSub = await mediator.Send(new GetActiveSubscriptionQuery(userId), ct);
        if (activeSub is null) return Results.NotFound(new { error = "No active subscription found." });

        var result = await mediator.Send(new CancelSubscriptionCommand(activeSub.SubscriptionId), ct);
        return Results.Ok(new { message = "Subscription auto-renewal canceled at end of current billing period.", success = result });
    }

    // ── Admin Handlers ────────────────────────────────────────────────────────

    private static async Task<IResult> GetAdminOverview(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminOverviewQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListAdminUsers(
        IMediator mediator, [Microsoft.AspNetCore.Mvc.FromQuery] string? search, [Microsoft.AspNetCore.Mvc.FromQuery] string? role, CancellationToken ct)
    {
        var result = await mediator.Send(new ListAdminUsersQuery(search, role), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ToggleUserSuspension(
        Guid id, [Microsoft.AspNetCore.Mvc.FromBody] ToggleSuspensionReq req, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ToggleUserSuspensionCommand(id, req.IsActive), ct);
        return Results.Ok(new { success = result });
    }

    private static async Task<IResult> ListAllCoupons(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListAllCouponsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateCoupon(
        CreateCouponRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCouponCommand(req, currentUser.UserId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ToggleCoupon(
        Guid id, [Microsoft.AspNetCore.Mvc.FromBody] ToggleCouponReq req, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ToggleCouponCommand(id, req.IsActive, req.IsVisiblePublicly), ct);
        return Results.Ok(new { success = result });
    }
}

public sealed record ToggleSuspensionReq(bool IsActive);
public sealed record ToggleCouponReq(bool? IsActive, bool? IsVisiblePublicly);
