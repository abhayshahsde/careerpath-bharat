namespace CareerPath.Contracts.V1.Billing;

public sealed record CheckoutSessionRequest(
    Guid PlanId,
    string PaymentProvider,   // Mock, Stripe, Razorpay
    string CardToken,         // Simulated payment method tokens
    string? CouponCode = null);

public sealed record CheckoutSessionResponse(
    bool Success,
    Guid SubscriptionId,
    string TransactionId,
    string Message,
    decimal OriginalPrice = 0,
    decimal DiscountApplied = 0,
    decimal FinalCharged = 0);

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string BillingCycle,
    int MaxDailyAiTokens,
    int MaxRoadmapsLimit);

public sealed record UserSubscriptionDto(
    Guid SubscriptionId,
    Guid PlanId,
    string PlanName,
    string Status,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    bool CancelAtPeriodEnd);

public sealed record CouponDto(
    Guid Id,
    string Code,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinPlanPrice,
    int MaxRedemptions,
    int TimesRedeemed,
    bool IsActive,
    bool IsVisiblePublicly,
    Guid? TargetUserId,
    string? TargetUserEmail,
    DateTimeOffset CreatedAt);

public sealed record ValidateCouponRequest(
    string Code,
    Guid PlanId);

public sealed record ValidateCouponResponse(
    bool IsValid,
    string Message,
    string? Code,
    string? DiscountType,
    decimal DiscountValue,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal FinalPrice);

public sealed record CreateCouponRequest(
    string Code,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinPlanPrice,
    int MaxRedemptions,
    bool IsActive,
    bool IsVisiblePublicly,
    Guid? TargetUserId);

public sealed record AdminOverviewDto(
    int TotalUsers,
    int ActiveUsersToday,
    int TotalSubscriptions,
    int ActiveSubscriptions,
    decimal MonthlyRecurringRevenue,
    decimal TotalRevenue,
    int TotalRoadmapsGenerated,
    int TotalAiQueriesServed,
    IReadOnlyList<SubscriptionTierCountDto> TierBreakdown);

public sealed record SubscriptionTierCountDto(
    string TierName,
    int SubscriberCount,
    decimal MonthlyRevenue);

public sealed record AdminUserRowDto(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    bool EmailVerified,
    string Role,
    string? SubscriptionTier,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
