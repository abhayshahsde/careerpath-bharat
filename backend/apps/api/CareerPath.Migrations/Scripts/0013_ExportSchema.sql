-- 0013: Export Schema — Export Jobs Tracking

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'export' AND t.name = 'ExportJobs')
BEGIN
    CREATE TABLE [export].[ExportJobs] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        ExportType      NVARCHAR(50) NOT NULL,            -- Careers, Roadmaps, Profile
        Format          NVARCHAR(10) NOT NULL,            -- PDF, XLSX, CSV, DOCX
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
        StoredPath      NVARCHAR(500) NULL,
        DownloadToken   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(), -- Used for expiring signed URL validation
        ExpireAt        DATETIMEOFFSET(7) NOT NULL,
        ErrorDetails    NVARCHAR(MAX) NULL,
        CreatedBy       UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAt     DATETIMEOFFSET(7) NULL,
        RowVersion      ROWVERSION
    );
    CREATE INDEX IX_ExportJobs_CreatedBy ON [export].[ExportJobs] (CreatedBy);
    CREATE INDEX IX_ExportJobs_ExpireAt ON [export].[ExportJobs] (ExpireAt);
END;
