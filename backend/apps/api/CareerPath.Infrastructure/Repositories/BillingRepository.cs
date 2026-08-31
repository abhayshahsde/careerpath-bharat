using Dapper;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Billing;
using CareerPath.Infrastructure.Data;

namespace CareerPath.Infrastructure.Repositories;

public sealed class BillingRepository : IBillingRepository
{
    private readonly ISqlConnectionFactory _db;
    public BillingRepository(ISqlConnectionFactory db) => _db = db;

    private sealed record PlanDbRow(
        Guid Id, string Name, decimal Price, string Currency, string BillingCycle, int MaxDailyAiTokens, int MaxRoadmapsLimit);

    private sealed record UserSubDbRow(
        Guid Id, Guid PlanId, string PlanName, string Status, DateTimeOffset CurrentPeriodStart, DateTimeOffset CurrentPeriodEnd, bool CancelAtPeriodEnd);

    public async Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<PlanDbRow>(
            """
            SELECT Id, Name, Price, Currency, BillingCycle, MaxDailyAiTokens, MaxRoadmapsLimit
            FROM [billing].[SubscriptionPlans]
            WHERE IsActive = 1
            """);

        return rows.Select(r => new SubscriptionPlanDto(
            r.Id, r.Name, r.Price, r.Currency, r.BillingCycle, r.MaxDailyAiTokens, r.MaxRoadmapsLimit)).ToList();
    }

    public async Task<SubscriptionPlanDto?> GetPlanAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var r = await conn.QuerySingleOrDefaultAsync<PlanDbRow>(
            """
            SELECT Id, Name, Price, Currency, BillingCycle, MaxDailyAiTokens, MaxRoadmapsLimit
            FROM [billing].[SubscriptionPlans]
            WHERE Id = @Id AND IsActive = 1
            """, new { Id = id });

        return r is null ? null : new SubscriptionPlanDto(
            r.Id, r.Name, r.Price, r.Currency, r.BillingCycle, r.MaxDailyAiTokens, r.MaxRoadmapsLimit);
    }

    public async Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<UserSubDbRow>(
            """
            SELECT s.Id, s.PlanId, p.Name AS PlanName, s.Status, s.CurrentPeriodStart, s.CurrentPeriodEnd, s.CancelAtPeriodEnd
            FROM [billing].[UserSubscriptions] s
            JOIN [billing].[SubscriptionPlans] p ON p.Id = s.PlanId
            WHERE s.UserId = @UserId AND s.Status = 'Active'
            """, new { UserId = userId });

        return row is null ? null : new UserSubscriptionDto(
            row.Id, row.PlanId, row.PlanName, row.Status, row.CurrentPeriodStart, row.CurrentPeriodEnd, row.CancelAtPeriodEnd);
    }

    public async Task<UserSubscriptionDto> CreateSubscriptionAsync(Guid userId, Guid planId, string provider, string txId, decimal amount, CancellationToken ct = default)
    {
        var plan = await GetPlanAsync(planId, ct) 
            ?? throw new KeyNotFoundException("Specified subscription plan was not found.");

        await using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        try
        {
            // Calculate Period dates
            var start = DateTimeOffset.UtcNow;
            var end = plan.BillingCycle == "Yearly" ? start.AddYears(1) : start.AddMonths(1);

            // 1. Check for existing subscription for the user to avoid UNIQUE constraint violations
            var existingSubId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT Id FROM [billing].[UserSubscriptions] WHERE UserId = @UserId",
                new { UserId = userId }, tx);

            Guid subId;
            if (existingSubId.HasValue)
            {
                subId = existingSubId.Value;
                await conn.ExecuteAsync(
                    """
                    UPDATE [billing].[UserSubscriptions]
                    SET PlanId = @PlanId,
                        Status = 'Active',
                        CurrentPeriodStart = @Start,
                        CurrentPeriodEnd = @End,
                        CancelAtPeriodEnd = 0,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id
                    """,
                    new { Id = subId, PlanId = planId, Start = start, End = end }, tx);
            }
            else
            {
                subId = await conn.ExecuteScalarAsync<Guid>(
                    """
                    INSERT INTO [billing].[UserSubscriptions] (UserId, PlanId, Status, CurrentPeriodStart, CurrentPeriodEnd, CancelAtPeriodEnd)
                    OUTPUT INSERTED.Id
                    VALUES (@UserId, @PlanId, 'Active', @Start, @End, 0)
                    """,
                    new { UserId = userId, PlanId = planId, Start = start, End = end }, tx);
            }

            // 3. Log transaction records
            await conn.ExecuteAsync(
                """
                INSERT INTO [billing].[PaymentTransactions] (UserId, SubscriptionId, Amount, Currency, Status, Provider, ProviderTxId)
                VALUES (@UserId, @SubId, @Amount, @Currency, 'Success', @Provider, @ProviderTxId)
                """,
                new { UserId = userId, SubId = subId, Amount = amount, Currency = plan.Currency, Provider = provider, ProviderTxId = txId }, tx);

            // 4. Update user's daily token entitlement quota limits inside ai.UserQuotas
            var quotaExists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM [ai].[UserQuotas] WHERE UserId = @UserId", new { UserId = userId }, tx);

            if (quotaExists > 0)
            {
                await conn.ExecuteAsync(
                    """
                    UPDATE [ai].[UserQuotas]
                    SET MaxDailyTokens = @MaxTokens,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE UserId = @UserId
                    """,
                    new { UserId = userId, MaxTokens = plan.MaxDailyAiTokens }, tx);
            }
            else
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO [ai].[UserQuotas] (UserId, MaxDailyTokens, UsedDailyTokens, ResetAt, UpdatedAt)
                    VALUES (@UserId, @MaxTokens, 0, DATEADD(day, 1, SYSUTCDATETIME()), SYSUTCDATETIME())
                    """,
                    new { UserId = userId, MaxTokens = plan.MaxDailyAiTokens }, tx);
            }

            tx.Commit();

            return new UserSubscriptionDto(subId, planId, plan.Name, "Active", start, end, false);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            UPDATE [billing].[UserSubscriptions]
            SET CancelAtPeriodEnd = 1,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @SubscriptionId
            """, new { SubscriptionId = subscriptionId });
    }

    // ── Coupons Implementation ────────────────────────────────────────────────

    private sealed class CouponDbRow
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "Percentage";
        public decimal DiscountValue { get; set; }
        public decimal MinPlanPrice { get; set; }
        public int MaxRedemptions { get; set; }
        public int TimesRedeemed { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisiblePublicly { get; set; }
        public Guid? TargetUserId { get; set; }
        public string? TargetUserEmail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    private sealed class SubscriptionTierDbRow
    {
        public string TierName { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    private sealed class AdminUserDbRow
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public string Role { get; set; } = "Student";
        public string? SubscriptionTier { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
    }

    public async Task<CouponDto?> GetCouponByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var r = await conn.QuerySingleOrDefaultAsync<CouponDbRow>(
            """
            SELECT c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue, c.MinPlanPrice,
                   c.MaxRedemptions, c.TimesRedeemed, c.IsActive, c.IsVisiblePublicly,
                   c.TargetUserId, u.Email AS TargetUserEmail, c.CreatedAt
            FROM [billing].[Coupons] c
            LEFT JOIN [identity].[Users] u ON u.Id = c.TargetUserId
            WHERE c.Code = @Code
            """, new { Code = code.Trim().ToUpperInvariant() });

        return r is null ? null : new CouponDto(
            r.Id, r.Code, r.Description, r.DiscountType, r.DiscountValue, r.MinPlanPrice,
            r.MaxRedemptions, r.TimesRedeemed, r.IsActive, r.IsVisiblePublicly,
            r.TargetUserId, r.TargetUserEmail, r.CreatedAt);
    }

    public async Task<IReadOnlyList<CouponDto>> ListPublicCouponsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<CouponDbRow>(
            """
            SELECT c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue, c.MinPlanPrice,
                   c.MaxRedemptions, c.TimesRedeemed, c.IsActive, c.IsVisiblePublicly,
                   c.TargetUserId, CAST(NULL AS NVARCHAR(256)) AS TargetUserEmail, c.CreatedAt
            FROM [billing].[Coupons] c
            WHERE c.IsActive = 1 AND c.IsVisiblePublicly = 1 AND c.TargetUserId IS NULL
            ORDER BY c.DiscountValue DESC
            """);

        return rows.Select(r => new CouponDto(
            r.Id, r.Code, r.Description, r.DiscountType, r.DiscountValue, r.MinPlanPrice,
            r.MaxRedemptions, r.TimesRedeemed, r.IsActive, r.IsVisiblePublicly,
            r.TargetUserId, r.TargetUserEmail, r.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<CouponDto>> ListAllCouponsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.QueryAsync<CouponDbRow>(
            """
            SELECT c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue, c.MinPlanPrice,
                   c.MaxRedemptions, c.TimesRedeemed, c.IsActive, c.IsVisiblePublicly,
                   c.TargetUserId, u.Email AS TargetUserEmail, c.CreatedAt
            FROM [billing].[Coupons] c
            LEFT JOIN [identity].[Users] u ON u.Id = c.TargetUserId
            ORDER BY c.CreatedAt DESC
            """);

        return rows.Select(r => new CouponDto(
            r.Id, r.Code, r.Description, r.DiscountType, r.DiscountValue, r.MinPlanPrice,
            r.MaxRedemptions, r.TimesRedeemed, r.IsActive, r.IsVisiblePublicly,
            r.TargetUserId, r.TargetUserEmail, r.CreatedAt)).ToList();
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponRequest req, Guid? createdBy, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var id = await conn.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO [billing].[Coupons] (Code, Description, DiscountType, DiscountValue, MinPlanPrice, MaxRedemptions, IsActive, IsVisiblePublicly, TargetUserId, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@Code, @Description, @DiscountType, @DiscountValue, @MinPlanPrice, @MaxRedemptions, @IsActive, @IsVisiblePublicly, @TargetUserId, @CreatedBy)
            """,
            new {
                Code = req.Code.Trim().ToUpperInvariant(),
                Description = req.Description,
                DiscountType = req.DiscountType,
                DiscountValue = req.DiscountValue,
                MinPlanPrice = req.MinPlanPrice,
                MaxRedemptions = req.MaxRedemptions,
                IsActive = req.IsActive,
                IsVisiblePublicly = req.IsVisiblePublicly,
                TargetUserId = req.TargetUserId,
                CreatedBy = createdBy
            });

        var created = await GetCouponByCodeAsync(req.Code, ct);
        return created!;
    }

    public async Task ToggleCouponStateAsync(Guid couponId, bool? isActive, bool? isVisiblePublicly, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        if (isActive.HasValue && isVisiblePublicly.HasValue)
        {
            await conn.ExecuteAsync(
                "UPDATE [billing].[Coupons] SET IsActive = @IsActive, IsVisiblePublicly = @IsVisiblePublicly WHERE Id = @Id",
                new { Id = couponId, IsActive = isActive.Value, IsVisiblePublicly = isVisiblePublicly.Value });
        }
        else if (isActive.HasValue)
        {
            await conn.ExecuteAsync(
                "UPDATE [billing].[Coupons] SET IsActive = @IsActive WHERE Id = @Id",
                new { Id = couponId, IsActive = isActive.Value });
        }
        else if (isVisiblePublicly.HasValue)
        {
            await conn.ExecuteAsync(
                "UPDATE [billing].[Coupons] SET IsVisiblePublicly = @IsVisiblePublicly WHERE Id = @Id",
                new { Id = couponId, IsVisiblePublicly = isVisiblePublicly.Value });
        }
    }

    public async Task RecordCouponRedemptionAsync(Guid couponId, Guid userId, Guid planId, decimal originalPrice, decimal discountAmount, decimal finalPrice, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            """
            INSERT INTO [billing].[CouponRedemptions] (CouponId, UserId, PlanId, OriginalPrice, DiscountAmount, FinalPrice)
            VALUES (@CouponId, @UserId, @PlanId, @OriginalPrice, @DiscountAmount, @FinalPrice);

            UPDATE [billing].[Coupons]
            SET TimesRedeemed = TimesRedeemed + 1
            WHERE Id = @CouponId;
            """,
            new { CouponId = couponId, UserId = userId, PlanId = planId, OriginalPrice = originalPrice, DiscountAmount = discountAmount, FinalPrice = finalPrice });
    }

    // ── Admin Analytics & Users Implementation ────────────────────────────────

    public async Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var totalUsers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [identity].[Users]");
        var activeUsersToday = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [identity].[Users] WHERE IsActive = 1");
        var totalSubs = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [billing].[UserSubscriptions]");
        var activeSubs = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [billing].[UserSubscriptions] WHERE Status = 'Active'");
        
        var totalRev = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(Amount) FROM [billing].[PaymentTransactions] WHERE Status = 'Success'") ?? 0m;
        
        var mrr = await conn.ExecuteScalarAsync<decimal?>(
            """
            SELECT SUM(p.Price)
            FROM [billing].[UserSubscriptions] s
            JOIN [billing].[SubscriptionPlans] p ON p.Id = s.PlanId
            WHERE s.Status = 'Active'
            """) ?? 0m;

        var totalRoadmaps = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [recommendation].[Roadmaps]");
        var totalAiPrompts = await conn.ExecuteScalarAsync<int?>("SELECT SUM(UsedDailyTokens) FROM [ai].[UserQuotas]") ?? 0;

        var tiers = await conn.QueryAsync<SubscriptionTierDbRow>(
            """
            SELECT p.Name AS TierName, COUNT(s.Id) AS SubscriberCount, ISNULL(SUM(p.Price), 0) AS MonthlyRevenue
            FROM [billing].[SubscriptionPlans] p
            LEFT JOIN [billing].[UserSubscriptions] s ON s.PlanId = p.Id AND s.Status = 'Active'
            GROUP BY p.Name, p.Price
            """);

        return new AdminOverviewDto(
            totalUsers,
            activeUsersToday,
            totalSubs,
            activeSubs,
            mrr,
            totalRev,
            totalRoadmaps,
            totalAiPrompts,
            tiers.Select(t => new SubscriptionTierCountDto(t.TierName, t.SubscriberCount, t.MonthlyRevenue)).ToList());
    }

    public async Task<IReadOnlyList<AdminUserRowDto>> ListAdminUsersAsync(string? search = null, string? role = null, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        var sql = """
            SELECT u.Id, u.Email, u.DisplayName, u.IsActive, u.IsEmailVerified AS EmailVerified,
                   ISNULL(STRING_AGG(r.Name, ','), 'Student') AS Role,
                   ISNULL(MAX(p.Name), 'Free Tier') AS SubscriptionTier,
                   u.CreatedAt, u.UpdatedAt AS LastLoginAt
            FROM [identity].[Users] u
            LEFT JOIN [identity].[UserRoles] ur ON ur.UserId = u.Id
            LEFT JOIN [identity].[Roles] r ON r.Id = ur.RoleId
            LEFT JOIN [billing].[UserSubscriptions] s ON s.UserId = u.Id AND s.Status = 'Active'
            LEFT JOIN [billing].[SubscriptionPlans] p ON p.Id = s.PlanId
            WHERE (@Search IS NULL OR u.Email LIKE '%' + @Search + '%' OR u.DisplayName LIKE '%' + @Search + '%')
            GROUP BY u.Id, u.Email, u.DisplayName, u.IsActive, u.IsEmailVerified, u.CreatedAt, u.UpdatedAt
            HAVING (@Role IS NULL OR CHARINDEX(@Role, ISNULL(STRING_AGG(r.Name, ','), 'Student')) > 0)
            ORDER BY u.CreatedAt DESC
            """;

        var rows = await conn.QueryAsync<AdminUserDbRow>(sql, new { Search = search, Role = role });
        return rows.Select(u => new AdminUserRowDto(
            u.Id, u.Email, u.DisplayName, u.IsActive, u.EmailVerified,
            u.Role, u.SubscriptionTier, u.CreatedAt, u.LastLoginAt)).ToList();
    }

    public async Task ToggleUserSuspensionAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        await using var conn = await _db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            "UPDATE [identity].[Users] SET IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = userId, IsActive = isActive });
    }
}
