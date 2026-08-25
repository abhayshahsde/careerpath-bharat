-- 0015: AI Schema — Quotas and Usage Logs

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'ai' AND t.name = 'UserQuotas')
BEGIN
    CREATE TABLE [ai].[UserQuotas] (
        UserId          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        MaxDailyTokens  INT NOT NULL DEFAULT 50000,
        UsedDailyTokens INT NOT NULL DEFAULT 0,
        ResetAt         DATETIMEOFFSET(7) NOT NULL DEFAULT DATEADD(day, 1, SYSUTCDATETIME()),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'ai' AND t.name = 'UsageLogs')
BEGIN
    CREATE TABLE [ai].[UsageLogs] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        RequestType     NVARCHAR(50) NOT NULL,            -- Chat, SemanticSearch, Translation
        ModelName       NVARCHAR(100) NOT NULL,
        PromptTokens    INT NOT NULL DEFAULT 0,
        CompletionTokens INT NOT NULL DEFAULT 0,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_UsageLogs_UserId_CreatedAt ON [ai].[UsageLogs] (UserId, CreatedAt DESC);
END;
