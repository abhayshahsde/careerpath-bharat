-- 0012: Import Schema — Staging, Validation, Review and Audit Logs

-- Import Jobs tracking
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'import' AND t.name = 'ImportJobs')
BEGIN
    CREATE TABLE [import].[ImportJobs] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        ImportType      NVARCHAR(50) NOT NULL,            -- Careers, Exams, Courses, Scholarships
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Created', -- Created, Validating, FailedValidation, Staged, Importing, Completed, Failed
        FileName        NVARCHAR(255) NOT NULL,
        StoredPath      NVARCHAR(500) NOT NULL,
        ContentType     NVARCHAR(100) NULL,
        FileSize        BIGINT NOT NULL DEFAULT 0,
        TotalRows       INT NOT NULL DEFAULT 0,
        ValidRows       INT NOT NULL DEFAULT 0,
        ErrorSummary    NVARCHAR(MAX) NULL,               -- JSON error summary
        CreatedBy       UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ProcessedAt     DATETIMEOFFSET(7) NULL,
        CompletedAt     DATETIMEOFFSET(7) NULL,
        RowVersion      ROWVERSION
    );
    CREATE INDEX IX_ImportJobs_CreatedBy ON [import].[ImportJobs] (CreatedBy);
    CREATE INDEX IX_ImportJobs_Status ON [import].[ImportJobs] (Status);
END;

-- Import Staging rows (temp storage for validation & review)
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'import' AND t.name = 'ImportStaging')
BEGIN
    CREATE TABLE [import].[ImportStaging] (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        JobId           UNIQUEIDENTIFIER NOT NULL REFERENCES [import].[ImportJobs](Id) ON DELETE CASCADE,
        RowIndex        INT NOT NULL,
        RowData         NVARCHAR(MAX) NOT NULL,           -- JSON representation of original row data
        RowStatus       NVARCHAR(20) NOT NULL DEFAULT 'Valid', -- Valid, Invalid
        ErrorMessage    NVARCHAR(MAX) NULL,               -- Validation error messages
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ImportStaging_JobId_Status ON [import].[ImportStaging] (JobId, RowStatus);
END;

-- Import Reviews & Approvals
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'import' AND t.name = 'ImportReviews')
BEGIN
    CREATE TABLE [import].[ImportReviews] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        JobId           UNIQUEIDENTIFIER NOT NULL REFERENCES [import].[ImportJobs](Id) ON DELETE CASCADE,
        ReviewedBy      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        IsApproved      BIT NOT NULL,
        Notes           NVARCHAR(1000) NULL,
        ReviewedAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ImportReviews_JobId ON [import].[ImportReviews] (JobId);
END;
