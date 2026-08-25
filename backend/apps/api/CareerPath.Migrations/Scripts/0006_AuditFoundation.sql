-- 0006: Audit foundation tables

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='audit' AND t.name='AuditEvents')
BEGIN
    CREATE TABLE [audit].[AuditEvents] (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        CorrelationId   NVARCHAR(50) NOT NULL,
        ActorId         UNIQUEIDENTIFIER NULL,
        ActorEmail      NVARCHAR(320) NULL,
        EventType       NVARCHAR(200) NOT NULL,
        EntityType      NVARCHAR(200) NULL,
        EntityId        NVARCHAR(200) NULL,
        OldValues       NVARCHAR(MAX) NULL,  -- JSON snapshot
        NewValues       NVARCHAR(MAX) NULL,  -- JSON snapshot
        IpAddress       NVARCHAR(50) NULL,
        OccurredAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AuditEvents_ActorId      ON [audit].[AuditEvents] (ActorId, OccurredAt DESC);
    CREATE INDEX IX_AuditEvents_EventType    ON [audit].[AuditEvents] (EventType, OccurredAt DESC);
    CREATE INDEX IX_AuditEvents_OccurredAt   ON [audit].[AuditEvents] (OccurredAt DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='audit' AND t.name='SecurityEvents')
BEGIN
    CREATE TABLE [audit].[SecurityEvents] (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        CorrelationId   NVARCHAR(50) NOT NULL,
        EventType       NVARCHAR(200) NOT NULL,
        Severity        NVARCHAR(50) NOT NULL DEFAULT 'Info',
        ActorId         UNIQUEIDENTIFIER NULL,
        IpAddress       NVARCHAR(50) NULL,
        Details         NVARCHAR(MAX) NULL,
        OccurredAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_SecurityEvents_OccurredAt ON [audit].[SecurityEvents] (OccurredAt DESC);
    CREATE INDEX IX_SecurityEvents_EventType  ON [audit].[SecurityEvents] (EventType, OccurredAt DESC);
END;
