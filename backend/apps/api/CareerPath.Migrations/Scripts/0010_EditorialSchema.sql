-- 0010: Editorial schema — content lifecycle management

-- Editorial schema: article/content management with full version history
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'editorial' AND t.name = 'Articles')
BEGIN
    CREATE TABLE [editorial].[Articles] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Slug            NVARCHAR(300) NOT NULL,
        ArticleType     NVARCHAR(50) NOT NULL,   -- 'CareerGuide','ExamGuide','ScholarshipGuide','CollegeProfile','BlogPost'
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Draft',  -- Draft,InReview,ChangesRequested,Approved,Published,Archived
        Locale          NVARCHAR(10) NOT NULL DEFAULT 'en',
        LinkedCareerId  UNIQUEIDENTIFIER NULL REFERENCES [catalog].[Careers](Id),
        AuthorId        UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        AssignedEditorId UNIQUEIDENTIFIER NULL REFERENCES [identity].[Users](Id),
        ScheduledAt     DATETIMEOFFSET(7) NULL,  -- scheduled publish time
        PublishedAt     DATETIMEOFFSET(7) NULL,
        ArchivedAt      DATETIMEOFFSET(7) NULL,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        RowVersion      ROWVERSION NOT NULL       -- optimistic concurrency
    );
    CREATE UNIQUE INDEX UX_Articles_Slug_Locale ON [editorial].[Articles] (Slug, Locale);
    CREATE INDEX IX_Articles_Status ON [editorial].[Articles] (Status);
    CREATE INDEX IX_Articles_AuthorId ON [editorial].[Articles] (AuthorId);
END;

-- Content versions — full text history with diff support
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'editorial' AND t.name = 'ContentVersions')
BEGIN
    CREATE TABLE [editorial].[ContentVersions] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ArticleId       UNIQUEIDENTIFIER NOT NULL REFERENCES [editorial].[Articles](Id) ON DELETE CASCADE,
        VersionNumber   INT NOT NULL,
        Title           NVARCHAR(500) NOT NULL,
        Summary         NVARCHAR(1000) NULL,
        Body            NVARCHAR(MAX) NOT NULL,  -- Markdown
        MetaDescription NVARCHAR(300) NULL,
        Keywords        NVARCHAR(500) NULL,
        ChangeNote      NVARCHAR(500) NULL,
        CreatedBy       UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        IsCurrentVersion BIT NOT NULL DEFAULT 0,
        WordCount       INT NOT NULL DEFAULT 0,
        ReadingTimeMinutes TINYINT NOT NULL DEFAULT 0
    );
    CREATE UNIQUE INDEX UX_ContentVersions_ArticleVersion ON [editorial].[ContentVersions] (ArticleId, VersionNumber);
    CREATE INDEX IX_ContentVersions_IsCurrent ON [editorial].[ContentVersions] (ArticleId, IsCurrentVersion);
END;

-- Review requests — editorial workflow
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'editorial' AND t.name = 'ReviewRequests')
BEGIN
    CREATE TABLE [editorial].[ReviewRequests] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ArticleId       UNIQUEIDENTIFIER NOT NULL REFERENCES [editorial].[Articles](Id),
        ContentVersionId INT NOT NULL REFERENCES [editorial].[ContentVersions](Id),
        ReviewerId      UNIQUEIDENTIFIER NULL REFERENCES [identity].[Users](Id),
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Pending',  -- Pending,InProgress,Approved,ChangesRequested,Rejected
        Feedback        NVARCHAR(2000) NULL,
        RequestedAt     DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ReviewedAt      DATETIMEOFFSET(7) NULL,
        DueBy           DATETIMEOFFSET(7) NULL
    );
    CREATE INDEX IX_ReviewRequests_ArticleId ON [editorial].[ReviewRequests] (ArticleId);
    CREATE INDEX IX_ReviewRequests_ReviewerId ON [editorial].[ReviewRequests] (ReviewerId, Status);
END;

-- Translation tracking — per article per locale
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'editorial' AND t.name = 'TranslationJobs')
BEGIN
    CREATE TABLE [editorial].[TranslationJobs] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ArticleId       UNIQUEIDENTIFIER NOT NULL REFERENCES [editorial].[Articles](Id),
        SourceLocale    NVARCHAR(10) NOT NULL DEFAULT 'en',
        TargetLocale    NVARCHAR(10) NOT NULL,
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Pending',  -- Pending,InProgress,Complete,Failed
        TranslatedBy    UNIQUEIDENTIFIER NULL REFERENCES [identity].[Users](Id),
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAt     DATETIMEOFFSET(7) NULL
    );
    CREATE UNIQUE INDEX UX_TranslationJobs_ArticleLocale ON [editorial].[TranslationJobs] (ArticleId, TargetLocale);
END;

-- Editorial audit log — who did what and when
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'editorial' AND t.name = 'EditorialEvents')
BEGIN
    CREATE TABLE [editorial].[EditorialEvents] (
        Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
        ArticleId   UNIQUEIDENTIFIER NOT NULL REFERENCES [editorial].[Articles](Id),
        ActorId     UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        EventType   NVARCHAR(100) NOT NULL,  -- 'Created','SubmittedForReview','Approved','ChangesRequested','Published','Archived','VersionSaved'
        Payload     NVARCHAR(MAX) NULL,      -- JSON snapshot
        OccurredAt  DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_EditorialEvents_ArticleId ON [editorial].[EditorialEvents] (ArticleId);
END;
