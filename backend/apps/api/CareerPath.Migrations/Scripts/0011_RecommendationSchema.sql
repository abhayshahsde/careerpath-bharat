-- 0011: Recommendations schema — deterministic scoring + roadmap/task progress

-- Career fit scores — precomputed per user, refreshed on profile update
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'recommendation' AND t.name = 'CareerScores')
BEGIN
    CREATE TABLE [recommendation].[CareerScores] (
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        CareerId        UNIQUEIDENTIFIER NOT NULL REFERENCES [catalog].[Careers](Id),
        Score           DECIMAL(5,2) NOT NULL,       -- 0.00 – 100.00
        SkillScore      DECIMAL(5,2) NOT NULL DEFAULT 0,
        EducationScore  DECIMAL(5,2) NOT NULL DEFAULT 0,
        InterestScore   DECIMAL(5,2) NOT NULL DEFAULT 0,
        Explanation     NVARCHAR(MAX) NULL,          -- JSON array of scored factors
        ComputedAt      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        ModelVersion    NVARCHAR(20) NOT NULL DEFAULT 'v1',
        PRIMARY KEY (UserId, CareerId)
    );
    CREATE INDEX IX_CareerScores_UserId_Score ON [recommendation].[CareerScores] (UserId, Score DESC);
END;

-- Roadmaps — a named learning/career plan for a user
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'recommendation' AND t.name = 'Roadmaps')
BEGIN
    CREATE TABLE [recommendation].[Roadmaps] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId          UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id) ON DELETE CASCADE,
        CareerId        UNIQUEIDENTIFIER NULL REFERENCES [catalog].[Careers](Id),
        Title           NVARCHAR(300) NOT NULL,
        Description     NVARCHAR(1000) NULL,
        Status          NVARCHAR(30) NOT NULL DEFAULT 'Active',  -- Active,Paused,Completed,Abandoned
        TargetDate      DATE NULL,
        CompletedAt     DATETIMEOFFSET(7) NULL,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Roadmaps_UserId ON [recommendation].[Roadmaps] (UserId, Status);
END;

-- Roadmap milestones — ordered phases within a roadmap
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'recommendation' AND t.name = 'Milestones')
BEGIN
    CREATE TABLE [recommendation].[Milestones] (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        RoadmapId   UNIQUEIDENTIFIER NOT NULL REFERENCES [recommendation].[Roadmaps](Id) ON DELETE CASCADE,
        Title       NVARCHAR(300) NOT NULL,
        Description NVARCHAR(1000) NULL,
        SortOrder   TINYINT NOT NULL DEFAULT 0,
        IsCompleted BIT NOT NULL DEFAULT 0,
        CompletedAt DATETIMEOFFSET(7) NULL
    );
    CREATE INDEX IX_Milestones_RoadmapId ON [recommendation].[Milestones] (RoadmapId, SortOrder);
END;

-- Tasks — actionable steps within a milestone
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'recommendation' AND t.name = 'Tasks')
BEGIN
    CREATE TABLE [recommendation].[Tasks] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        MilestoneId     INT NOT NULL REFERENCES [recommendation].[Milestones](Id) ON DELETE CASCADE,
        Title           NVARCHAR(500) NOT NULL,
        Description     NVARCHAR(2000) NULL,
        TaskType        NVARCHAR(50) NOT NULL DEFAULT 'General',  -- General,StudyMaterial,ExamPrep,CourseEnrollment,SkillPractice
        LinkedExamId    INT NULL REFERENCES [catalog].[Exams](Id),
        LinkedCourseId  INT NULL REFERENCES [catalog].[Courses](Id),
        LinkedSkillId   INT NULL REFERENCES [catalog].[Skills](Id),
        ExternalUrl     NVARCHAR(2000) NULL,
        SortOrder       TINYINT NOT NULL DEFAULT 0,
        IsCompleted     BIT NOT NULL DEFAULT 0,
        CompletedAt     DATETIMEOFFSET(7) NULL,
        DueDate         DATE NULL
    );
    CREATE INDEX IX_Tasks_MilestoneId ON [recommendation].[Tasks] (MilestoneId, SortOrder);
END;

-- Scoring factors config — weights used in deterministic algorithm (admin-managed)
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'recommendation' AND t.name = 'ScoringConfig')
BEGIN
    CREATE TABLE [recommendation].[ScoringConfig] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FactorKey       NVARCHAR(100) NOT NULL UNIQUE,   -- 'skill_match','education_match','interest_match'
        Weight          DECIMAL(5,2) NOT NULL,           -- must sum to 100 across active factors
        IsActive        BIT NOT NULL DEFAULT 1,
        Description     NVARCHAR(500) NULL,
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- Default weights (skill 50%, education 30%, interest 20%)
    IF NOT EXISTS (SELECT 1 FROM [recommendation].[ScoringConfig] WHERE FactorKey = 'skill_match')
    BEGIN
        INSERT INTO [recommendation].[ScoringConfig] (FactorKey, Weight, Description)
        VALUES
            ('skill_match',      50.00, 'Score based on user skills vs career required skills'),
            ('education_match',  30.00, 'Score based on current education vs career minimum'),
            ('interest_match',   20.00, 'Score based on career category matching user interests');
    END;
END;
