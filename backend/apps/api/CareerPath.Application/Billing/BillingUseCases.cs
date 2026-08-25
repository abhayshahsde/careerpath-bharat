using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Billing;

namespace CareerPath.Application.Billing;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record ListPlansQuery : IRequest<IReadOnlyList<SubscriptionPlanDto>>;

public sealed class ListPlansHandler : IRequestHandler<ListPlansQuery, IReadOnlyList<SubscriptionPlanDto>>
{
    private readonly IBillingRepository _repo;
    public ListPlansHandler(IBillingRepository repo) => _repo = repo;
    public Task<IReadOnlyList<SubscriptionPlanDto>> Handle(ListPlansQuery q, CancellationToken ct)
        => _repo.ListPlansAsync(ct);
}

public sealed record GetActiveSubscriptionQuery(Guid UserId) : IRequest<UserSubscriptionDto?>;

public sealed class GetActiveSubscriptionHandler : IRequestHandler<GetActiveSubscriptionQuery, UserSubscriptionDto?>
{
    private readonly IBillingRepository _repo;
    public GetActiveSubscriptionHandler(IBillingRepository repo) => _repo = repo;
    public Task<UserSubscriptionDto?> Handle(GetActiveSubscriptionQuery q, CancellationToken ct)
        => _repo.GetActiveSubscriptionAsync(q.UserId, ct);
}

// ── Subscribe Checkout Command ───────────────────────────────────────────────

public sealed record SubscribeCommand(
    Guid UserId, Guid PlanId, string Provider, string CardToken, string? CouponCode = null) : IRequest<CheckoutSessionResponse>;

public sealed class SubscribeValidator : AbstractValidator<SubscribeCommand>
{
    private static readonly string[] AllowedProviders = ["Mock", "Stripe", "Razorpay", "Free"];

    public SubscribeValidator()
    {
        RuleFor(x => x.Provider).Must(p => AllowedProviders.Contains(p))
            .WithMessage($"Payment Provider must be one of: {string.Join(", ", AllowedProviders)}");
        RuleFor(x => x.CardToken).NotEmpty()
            .WithMessage("A valid payment token is required.");
    }
}

public sealed class SubscribeHandler : IRequestHandler<SubscribeCommand, CheckoutSessionResponse>
{
    private readonly IBillingRepository _repo;
    public SubscribeHandler(IBillingRepository repo) => _repo = repo;

    public async Task<CheckoutSessionResponse> Handle(SubscribeCommand cmd, CancellationToken ct)
    {
        var plan = await _repo.GetPlanAsync(cmd.PlanId, ct);
        if (plan is null)
        {
            throw new KeyNotFoundException("The requested subscription plan does not exist.");
        }

        decimal originalPrice = plan.Price;
        decimal discountAmount = 0m;
        decimal finalPrice = originalPrice;
        CouponDto? coupon = null;

        if (!string.IsNullOrWhiteSpace(cmd.CouponCode))
        {
            coupon = await _repo.GetCouponByCodeAsync(cmd.CouponCode, ct);
            if (coupon != null && coupon.IsActive)
            {
                if (coupon.TargetUserId == null || coupon.TargetUserId == cmd.UserId)
                {
                    if (originalPrice >= coupon.MinPlanPrice)
                    {
                        if (coupon.DiscountType == "Percentage")
                        {
                            discountAmount = Math.Round((originalPrice * coupon.DiscountValue) / 100m, 2);
                        }
                        else
                        {
                            discountAmount = Math.Min(originalPrice, coupon.DiscountValue);
                        }
                        finalPrice = Math.Max(0m, originalPrice - discountAmount);
                    }
                }
            }
        }

        var txId = $"tx_{cmd.Provider.ToLower()}_{Guid.NewGuid():N}";
        var subscription = await _repo.CreateSubscriptionAsync(
            cmd.UserId, cmd.PlanId, cmd.Provider, txId, finalPrice, ct);

        if (coupon != null && discountAmount > 0)
        {
            await _repo.RecordCouponRedemptionAsync(
                coupon.Id, cmd.UserId, cmd.PlanId, originalPrice, discountAmount, finalPrice, ct);
        }

        return new CheckoutSessionResponse(
            true,
            subscription.SubscriptionId,
            txId,
            $"Successfully activated {plan.Name} subscription plan.",
            originalPrice,
            discountAmount,
            finalPrice);
    }
}

// ── Cancel Subscription Command ──────────────────────────────────────────────

public sealed record CancelSubscriptionCommand(Guid SubscriptionId) : IRequest<bool>;

public sealed class CancelSubscriptionHandler : IRequestHandler<CancelSubscriptionCommand, bool>
{
    private readonly IBillingRepository _repo;
    public CancelSubscriptionHandler(IBillingRepository repo) => _repo = repo;

    public async Task<bool> Handle(CancelSubscriptionCommand cmd, CancellationToken ct)
    {
        await _repo.CancelSubscriptionAsync(cmd.SubscriptionId, ct);
        return true;
    }
}

// ── Coupons Queries & Commands ───────────────────────────────────────────────

public sealed record ValidateCouponQuery(Guid UserId, string Code, Guid PlanId) : IRequest<ValidateCouponResponse>;

public sealed class ValidateCouponHandler : IRequestHandler<ValidateCouponQuery, ValidateCouponResponse>
{
    private readonly IBillingRepository _repo;
    public ValidateCouponHandler(IBillingRepository repo) => _repo = repo;

    public async Task<ValidateCouponResponse> Handle(ValidateCouponQuery q, CancellationToken ct)
    {
        var plan = await _repo.GetPlanAsync(q.PlanId, ct);
        if (plan is null)
            return new ValidateCouponResponse(false, "Invalid subscription plan.", null, null, 0, 0, 0, 0);

        var coupon = await _repo.GetCouponByCodeAsync(q.Code, ct);
        if (coupon is null)
            return new ValidateCouponResponse(false, "Coupon code not found.", null, null, 0, plan.Price, 0, plan.Price);

        if (!coupon.IsActive)
            return new ValidateCouponResponse(false, "This coupon is inactive or has expired.", coupon.Code, null, 0, plan.Price, 0, plan.Price);

        if (coupon.TimesRedeemed >= coupon.MaxRedemptions)
            return new ValidateCouponResponse(false, "This coupon has reached its maximum redemption limit.", coupon.Code, null, 0, plan.Price, 0, plan.Price);

        if (coupon.TargetUserId.HasValue && coupon.TargetUserId.Value != q.UserId)
            return new ValidateCouponResponse(false, "This coupon is exclusively assigned to another account.", coupon.Code, null, 0, plan.Price, 0, plan.Price);

        if (plan.Price < coupon.MinPlanPrice)
            return new ValidateCouponResponse(false, $"Minimum plan amount for this coupon is ₹{coupon.MinPlanPrice:N0}.", coupon.Code, null, 0, plan.Price, 0, plan.Price);

        decimal discount = 0m;
        if (coupon.DiscountType == "Percentage")
            discount = Math.Round((plan.Price * coupon.DiscountValue) / 100m, 2);
        else
            discount = Math.Min(plan.Price, coupon.DiscountValue);

        decimal finalPrice = Math.Max(0m, plan.Price - discount);

        return new ValidateCouponResponse(
            true,
            $"Coupon applied! You saved ₹{discount:N0}.",
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            plan.Price,
            discount,
            finalPrice);
    }
}

public sealed record ListPublicCouponsQuery : IRequest<IReadOnlyList<CouponDto>>;

public sealed class ListPublicCouponsHandler : IRequestHandler<ListPublicCouponsQuery, IReadOnlyList<CouponDto>>
{
    private readonly IBillingRepository _repo;
    public ListPublicCouponsHandler(IBillingRepository repo) => _repo = repo;
    public Task<IReadOnlyList<CouponDto>> Handle(ListPublicCouponsQuery q, CancellationToken ct)
        => _repo.ListPublicCouponsAsync(ct);
}

public sealed record ListAllCouponsQuery : IRequest<IReadOnlyList<CouponDto>>;

public sealed class ListAllCouponsHandler : IRequestHandler<ListAllCouponsQuery, IReadOnlyList<CouponDto>>
{
    private readonly IBillingRepository _repo;
    public ListAllCouponsHandler(IBillingRepository repo) => _repo = repo;
    public Task<IReadOnlyList<CouponDto>> Handle(ListAllCouponsQuery q, CancellationToken ct)
        => _repo.ListAllCouponsAsync(ct);
}

public sealed record CreateCouponCommand(CreateCouponRequest Request, Guid? CreatedBy) : IRequest<CouponDto>;

public sealed class CreateCouponHandler : IRequestHandler<CreateCouponCommand, CouponDto>
{
    private readonly IBillingRepository _repo;
    public CreateCouponHandler(IBillingRepository repo) => _repo = repo;
    public Task<CouponDto> Handle(CreateCouponCommand cmd, CancellationToken ct)
        => _repo.CreateCouponAsync(cmd.Request, cmd.CreatedBy, ct);
}

public sealed record ToggleCouponCommand(Guid CouponId, bool? IsActive, bool? IsVisiblePublicly) : IRequest<bool>;

public sealed class ToggleCouponHandler : IRequestHandler<ToggleCouponCommand, bool>
{
    private readonly IBillingRepository _repo;
    public ToggleCouponHandler(IBillingRepository repo) => _repo = repo;
    public async Task<bool> Handle(ToggleCouponCommand cmd, CancellationToken ct)
    {
        await _repo.ToggleCouponStateAsync(cmd.CouponId, cmd.IsActive, cmd.IsVisiblePublicly, ct);
        return true;
    }
}

// ── Admin Analytics Queries & Commands ───────────────────────────────────────

public sealed record GetAdminOverviewQuery : IRequest<AdminOverviewDto>;

public sealed class GetAdminOverviewHandler : IRequestHandler<GetAdminOverviewQuery, AdminOverviewDto>
{
    private readonly IBillingRepository _repo;
    public GetAdminOverviewHandler(IBillingRepository repo) => _repo = repo;
    public Task<AdminOverviewDto> Handle(GetAdminOverviewQuery q, CancellationToken ct)
        => _repo.GetAdminOverviewAsync(ct);
}

public sealed record ListAdminUsersQuery(string? Search, string? Role) : IRequest<IReadOnlyList<AdminUserRowDto>>;

public sealed class ListAdminUsersHandler : IRequestHandler<ListAdminUsersQuery, IReadOnlyList<AdminUserRowDto>>
{
    private readonly IBillingRepository _repo;
    public ListAdminUsersHandler(IBillingRepository repo) => _repo = repo;
    public Task<IReadOnlyList<AdminUserRowDto>> Handle(ListAdminUsersQuery q, CancellationToken ct)
        => _repo.ListAdminUsersAsync(q.Search, q.Role, ct);
}

public sealed record ToggleUserSuspensionCommand(Guid UserId, bool IsActive) : IRequest<bool>;

public sealed class ToggleUserSuspensionHandler : IRequestHandler<ToggleUserSuspensionCommand, bool>
{
    private readonly IBillingRepository _repo;
    public ToggleUserSuspensionHandler(IBillingRepository repo) => _repo = repo;
    public async Task<bool> Handle(ToggleUserSuspensionCommand cmd, CancellationToken ct)
    {
        await _repo.ToggleUserSuspensionAsync(cmd.UserId, cmd.IsActive, ct);
        return true;
    }
}
