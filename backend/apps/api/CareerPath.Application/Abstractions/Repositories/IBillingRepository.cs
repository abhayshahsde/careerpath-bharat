using CareerPath.Contracts.V1.Billing;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IBillingRepository
{
    Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default);
    Task<SubscriptionPlanDto?> GetPlanAsync(Guid id, CancellationToken ct = default);
    Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<UserSubscriptionDto> CreateSubscriptionAsync(Guid userId, Guid planId, string provider, string txId, decimal amount, CancellationToken ct = default);
    Task CancelSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);

    // Coupons
    Task<CouponDto?> GetCouponByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<CouponDto>> ListPublicCouponsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CouponDto>> ListAllCouponsAsync(CancellationToken ct = default);
    Task<CouponDto> CreateCouponAsync(CreateCouponRequest req, Guid? createdBy, CancellationToken ct = default);
    Task ToggleCouponStateAsync(Guid couponId, bool? isActive, bool? isVisiblePublicly, CancellationToken ct = default);
    Task RecordCouponRedemptionAsync(Guid couponId, Guid userId, Guid planId, decimal originalPrice, decimal discountAmount, decimal finalPrice, CancellationToken ct = default);

    // Admin Metrics & Users
    Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminUserRowDto>> ListAdminUsersAsync(string? search = null, string? role = null, CancellationToken ct = default);
    Task ToggleUserSuspensionAsync(Guid userId, bool isActive, CancellationToken ct = default);
}
