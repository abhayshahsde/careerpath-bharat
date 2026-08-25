-- 0017: Notifications & Analytics Schema

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'system' AND t.name = 'Notifications')
BEGIN
    CREATE TABLE [system].[Notifications] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        Title           NVARCHAR(200) NOT NULL,
        Message         NVARCHAR(1000) NOT NULL,
        Type            NVARCHAR(30) NOT NULL DEFAULT 'Info', -- Success, Warning, Alert, Info
        IsRead          BIT NOT NULL DEFAULT 0,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ReadAt          DATETIMEOFFSET(7) NULL
    );
    CREATE INDEX IX_Notifications_UserId ON [system].[Notifications] (UserId);
    CREATE INDEX IX_Notifications_IsRead ON [system].[Notifications] (IsRead);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'audit' AND t.name = 'TelemetryEvents')
BEGIN
    CREATE TABLE [audit].[TelemetryEvents] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NULL REFERENCES [identity].[Users](Id) ON DELETE SET NULL,
        EventName       NVARCHAR(100) NOT NULL,
        Payload         NVARCHAR(MAX) NULL,               -- JSON event metadata
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_TelemetryEvents_UserId ON [audit].[TelemetryEvents] (UserId);
    CREATE INDEX IX_TelemetryEvents_EventName ON [audit].[TelemetryEvents] (EventName);
END;
