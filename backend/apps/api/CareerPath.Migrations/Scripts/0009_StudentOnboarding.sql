-- 0009: Student Onboarding + Privacy controls

-- Onboarding wizard answers
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'student' AND t.name = 'OnboardingAnswers')
BEGIN
    CREATE TABLE [student].[OnboardingAnswers] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL
                            REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        Step            TINYINT NOT NULL,   -- 1=Education, 2=Interests, 3=Goals, 4=Location
        QuestionKey     NVARCHAR(100) NOT NULL,
        Answer          NVARCHAR(1000) NOT NULL,
        AnsweredAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_OnboardingAnswers_UserId ON [student].[OnboardingAnswers] (UserId);
    CREATE UNIQUE INDEX UX_OnboardingAnswers_UserQuestion ON [student].[OnboardingAnswers] (UserId, QuestionKey);
END;

-- Career interest preferences
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'student' AND t.name = 'CareerInterests')
BEGIN
    CREATE TABLE [student].[CareerInterests] (
        UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        CategoryId  NVARCHAR(100) NOT NULL REFERENCES [catalog].[Categories](Id),
        Rank        TINYINT NOT NULL DEFAULT 0,  -- 0=interested, 1=top priority
        CreatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        PRIMARY KEY (UserId, CategoryId)
    );
END;

-- Privacy consent records
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'student' AND t.name = 'ConsentRecords')
BEGIN
    CREATE TABLE [student].[ConsentRecords] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        ConsentType     NVARCHAR(100) NOT NULL,   -- 'DataProcessing','MarketingEmails','AnalyticsCookies','ThirdPartySharing'
        Granted         BIT NOT NULL,
        RecordedAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        IpAddress       NVARCHAR(50) NULL,
        UserAgent       NVARCHAR(500) NULL,
        ConsentVersion  NVARCHAR(20) NOT NULL DEFAULT '1.0'
    );
    CREATE INDEX IX_ConsentRecords_UserId ON [student].[ConsentRecords] (UserId, ConsentType);
END;

-- Data deletion requests (GDPR / PDPB India)
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'student' AND t.name = 'DataDeletionRequests')
BEGIN
    CREATE TABLE [student].[DataDeletionRequests] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        RequestedAt     DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ScheduledFor    DATETIMEOFFSET(7) NOT NULL,   -- 30 days from request
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Pending',  -- Pending,Processing,Completed,Cancelled
        ProcessedAt     DATETIMEOFFSET(7) NULL,
        Notes           NVARCHAR(500) NULL
    );
END;
