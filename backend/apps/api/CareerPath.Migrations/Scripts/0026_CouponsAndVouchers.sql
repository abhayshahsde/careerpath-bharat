-- 0026: Coupons and Promotional Vouchers Schema

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'billing' AND t.name = 'Coupons')
BEGIN
    CREATE TABLE [billing].[Coupons] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Code            NVARCHAR(50) NOT NULL UNIQUE,
        Description     NVARCHAR(250) NULL,
        DiscountType    NVARCHAR(20) NOT NULL DEFAULT 'Percentage', -- 'Percentage' or 'FixedAmount'
        DiscountValue   DECIMAL(18, 2) NOT NULL,
        MinPlanPrice    DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
        MaxRedemptions  INT NOT NULL DEFAULT 100,
        TimesRedeemed   INT NOT NULL DEFAULT 0,
        ValidFrom       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ValidTo         DATETIMEOFFSET(7) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        IsVisiblePublicly BIT NOT NULL DEFAULT 1,
        TargetUserId    UNIQUEIDENTIFIER NULL REFERENCES [identity].[Users](Id) ON DELETE SET NULL,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_Coupons_Code ON [billing].[Coupons](Code);
    CREATE INDEX IX_Coupons_TargetUserId ON [billing].[Coupons](TargetUserId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'billing' AND t.name = 'CouponRedemptions')
BEGIN
    CREATE TABLE [billing].[CouponRedemptions] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        CouponId        UNIQUEIDENTIFIER NOT NULL REFERENCES [billing].[Coupons](Id) ON DELETE CASCADE,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        PlanId          UNIQUEIDENTIFIER NOT NULL REFERENCES [billing].[SubscriptionPlans](Id),
        OriginalPrice   DECIMAL(18, 2) NOT NULL,
        DiscountAmount  DECIMAL(18, 2) NOT NULL,
        FinalPrice      DECIMAL(18, 2) NOT NULL,
        RedeemedAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_CouponRedemptions_UserId ON [billing].[CouponRedemptions](UserId);
    CREATE INDEX IX_CouponRedemptions_CouponId ON [billing].[CouponRedemptions](CouponId);
END;

-- Seed initial promotional vouchers
IF NOT EXISTS (SELECT 1 FROM [billing].[Coupons] WHERE Code = 'BHARAT50')
BEGIN
    INSERT INTO [billing].[Coupons] (Code, Description, DiscountType, DiscountValue, MinPlanPrice, MaxRedemptions, IsActive, IsVisiblePublicly)
    VALUES 
    ('BHARAT50', '50% Welcome Discount for Indian Students', 'Percentage', 50.00, 100.00, 500, 1, 1),
    ('SPECIAL100', 'Flat Rs. 100 Off on Pro and Premium Plans', 'FixedAmount', 100.00, 200.00, 200, 1, 1),
    ('STUDENTVIP', 'Exclusive 80% Off Voucher for Scholarship Winners', 'Percentage', 80.00, 100.00, 50, 1, 0);
END;
