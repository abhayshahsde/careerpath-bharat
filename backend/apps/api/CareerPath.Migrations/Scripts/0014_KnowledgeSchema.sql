-- 0014: Knowledge Schema — Documents Staging, Extraction, Chunks and Review

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'knowledge' AND t.name = 'Documents')
BEGIN
    CREATE TABLE [knowledge].[Documents] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Title           NVARCHAR(300) NOT NULL,
        DocType         NVARCHAR(50) NOT NULL,            -- Syllabus, ExamNotification, Policy, CareerGuideline
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- Pending, Extracting, Staged, Reviewing, Indexed, Failed
        FilePath        NVARCHAR(500) NOT NULL,
        FileSize        BIGINT NOT NULL DEFAULT 0,
        ErrorDetails    NVARCHAR(MAX) NULL,
        CreatedBy       UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        RowVersion      ROWVERSION
    );
    CREATE INDEX IX_Documents_CreatedBy ON [knowledge].[Documents] (CreatedBy);
    CREATE INDEX IX_Documents_Status ON [knowledge].[Documents] (Status);
END;

-- Document text chunks (for vector search indexing)
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'knowledge' AND t.name = 'DocumentChunks')
BEGIN
    CREATE TABLE [knowledge].[DocumentChunks] (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        DocumentId      UNIQUEIDENTIFIER NOT NULL REFERENCES [knowledge].[Documents](Id) ON DELETE CASCADE,
        ChunkIndex      INT NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        TokenCount      INT NOT NULL DEFAULT 0,
        IsReviewed      BIT NOT NULL DEFAULT 0,
        VectorRefId     NVARCHAR(100) NULL,               -- Maps to external vector DB ID (AI module target)
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_DocumentChunks_DocId_Index ON [knowledge].[DocumentChunks] (DocumentId, ChunkIndex);
    CREATE INDEX IX_DocumentChunks_VectorRef ON [knowledge].[DocumentChunks] (VectorRefId);
END;

-- Document Reviews & Approvals log
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'knowledge' AND t.name = 'DocumentReviews')
BEGIN
    CREATE TABLE [knowledge].[DocumentReviews] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        DocumentId      UNIQUEIDENTIFIER NOT NULL REFERENCES [knowledge].[Documents](Id) ON DELETE CASCADE,
        ReviewedBy      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        IsApproved      BIT NOT NULL,
        Notes           NVARCHAR(1000) NULL,
        ReviewedAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_DocumentReviews_DocId ON [knowledge].[DocumentReviews] (DocumentId);
END;
