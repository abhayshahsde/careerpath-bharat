-- 0016: Billing Schema — Subscription Plans, User Subscriptions, Payments

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'billing' AND t.name = 'SubscriptionPlans')
BEGIN
    CREATE TABLE [billing].[SubscriptionPlans] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Name            NVARCHAR(100) NOT NULL,
        Price           DECIMAL(18, 2) NOT NULL,
        Currency        NVARCHAR(10) NOT NULL DEFAULT 'INR',
        BillingCycle    NVARCHAR(20) NOT NULL,            -- Monthly, Yearly
        MaxDailyAiTokens INT NOT NULL DEFAULT 50000,
        MaxRoadmapsLimit INT NOT NULL DEFAULT 3,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'billing' AND t.name = 'UserSubscriptions')
BEGIN
    CREATE TABLE [billing].[UserSubscriptions] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL UNIQUE REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        PlanId          UNIQUEIDENTIFIER NOT NULL REFERENCES [billing].[SubscriptionPlans](Id),
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Active', -- Active, Canceled, Expired
        CurrentPeriodStart DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CurrentPeriodEnd   DATETIMEOFFSET(7) NOT NULL,
        CancelAtPeriodEnd  BIT NOT NULL DEFAULT 0,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        RowVersion      ROWVERSION
    );
    CREATE INDEX IX_UserSubscriptions_UserId ON [billing].[UserSubscriptions] (UserId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'billing' AND t.name = 'PaymentTransactions')
BEGIN
    CREATE TABLE [billing].[PaymentTransactions] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        SubscriptionId  UNIQUEIDENTIFIER NULL REFERENCES [billing].[UserSubscriptions](Id) ON DELETE SET NULL,
        Amount          DECIMAL(18, 2) NOT NULL,
        Currency        NVARCHAR(10) NOT NULL DEFAULT 'INR',
        Status          NVARCHAR(30) NOT NULL,            -- Success, Pending, Failed
        Provider        NVARCHAR(50) NOT NULL DEFAULT 'Mock', -- Razorpay, Stripe, Mock
        ProviderTxId    NVARCHAR(200) NOT NULL,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_PaymentTransactions_UserId ON [billing].[PaymentTransactions] (UserId);
END;

-- Seed Plans
IF NOT EXISTS (SELECT 1 FROM [billing].[SubscriptionPlans])
BEGIN
    INSERT INTO [billing].[SubscriptionPlans] (Name, Price, Currency, BillingCycle, MaxDailyAiTokens, MaxRoadmapsLimit, IsActive)
    VALUES 
    ('Free Tier', 0.00, 'INR', 'Monthly', 10000, 2, 1),
    ('Pro Path', 499.00, 'INR', 'Monthly', 100000, 10, 1),
    ('Premium Elite', 4999.00, 'INR', 'Yearly', 500000, 999, 1);
END;
