-- 0028: Admin CMS and Dynamic Site Settings
-- Schema: [settings]

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'settings')
BEGIN
    EXEC('CREATE SCHEMA [settings]');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'settings' AND t.name = 'SiteSettings')
BEGIN
    CREATE TABLE [settings].[SiteSettings] (
        Id                  INT PRIMARY KEY DEFAULT 1,
        SiteName            NVARCHAR(100) NOT NULL DEFAULT 'CareerPath Bharat',
        LogoText            NVARCHAR(100) NOT NULL DEFAULT 'CareerPath',
        LogoSubtitle        NVARCHAR(50) NOT NULL DEFAULT 'Bharat',
        Tagline             NVARCHAR(255) NOT NULL DEFAULT 'India''s premier career guidance and roadmapping platform for students',
        AnnouncementText    NVARCHAR(500) NULL DEFAULT '⚡ UPSC, JEE & NEET 2026 notifications out now! Check your personalized roadmaps.',
        AnnouncementActive  BIT NOT NULL DEFAULT 1,
        SupportEmail        NVARCHAR(255) NOT NULL DEFAULT 'support@careerpathbharat.com',
        SupportPhone        NVARCHAR(50) NOT NULL DEFAULT '+91 9876543210',
        FooterText          NVARCHAR(500) NOT NULL DEFAULT 'Empowering students across all 28 states & 8 UTs of Bharat.',
        NavMenusJson        NVARCHAR(MAX) NOT NULL DEFAULT '[{"label":"Dashboard","href":"/dashboard","isActive":true},{"label":"Roadmaps","href":"/me/roadmaps","isActive":true},{"label":"Careers","href":"/careers","isActive":true},{"label":"Exams","href":"/exams","isActive":true},{"label":"Courses","href":"/courses","isActive":true},{"label":"Scholarships","href":"/scholarships","isActive":true}]',
        UpdatedAt           DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO [settings].[SiteSettings] (Id) VALUES (1);
END;

-- Seed initial sample documents for Knowledge Base if empty
IF NOT EXISTS (SELECT 1 FROM [knowledge].[Documents])
BEGIN
    DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [identity].[Users] ORDER BY CreatedAt ASC);
    IF @AdminUserId IS NULL SET @AdminUserId = NEWID();

    DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [knowledge].[Documents] (Id, Title, DocType, Status, FilePath, FileSize, CreatedBy)
    VALUES (@DocId, 'UPSC Civil Services Syllabus & Eligibility 2026', 'Syllabus', 'Indexed', '/knowledge/upsc_2026_syllabus.pdf', 48200, @AdminUserId);

    INSERT INTO [knowledge].[DocumentChunks] (DocumentId, ChunkIndex, Content, TokenCount, IsReviewed)
    VALUES 
    (@DocId, 0, 'UPSC CSE consists of three stages: Preliminary Examination (Objective), Main Examination (Written), and Personality Test (Interview). Minimum age is 21 years and graduate degree in any discipline from a recognized university.', 42, 1),
    (@DocId, 1, 'General Studies Paper I covers History of India, Indian National Movement, Indian and World Geography, Indian Polity and Governance, Economic and Social Development, Environmental Ecology, and General Science.', 38, 1);
END;

-- Seed initial sample Editorial Articles if empty
IF NOT EXISTS (SELECT 1 FROM [editorial].[Articles])
BEGIN
    DECLARE @AuthorId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [identity].[Users] ORDER BY CreatedAt ASC);
    IF @AuthorId IS NULL SET @AuthorId = NEWID();

    DECLARE @Article1Id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [editorial].[Articles] (Id, Slug, ArticleType, Status, Locale, AuthorId, PublishedAt, CreatedAt, UpdatedAt)
    VALUES (@Article1Id, 'jee-advanced-class-11-prep', 'CareerGuide', 'InReview', 'en', @AuthorId, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO [editorial].[ContentVersions] (ArticleId, VersionNumber, Title, Summary, Body, CreatedBy, IsCurrentVersion, WordCount, ReadingTimeMinutes)
    VALUES (@Article1Id, 1, 'How to Prepare for JEE Advanced from Class 11', 'A comprehensive step-by-step roadmap for engineering aspirants tackling Physics, Chemistry, and Math from day one.', 'Focus on foundational NCERT concepts, followed by Irodov for Physics and MS Chouhan for Organic Chemistry. Solve past 15 years question papers systematically.', @AuthorId, 1, 350, 3);

    DECLARE @Article2Id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [editorial].[Articles] (Id, Slug, ArticleType, Status, Locale, AuthorId, PublishedAt, CreatedAt, UpdatedAt)
    VALUES (@Article2Id, 'medical-vs-biotech-2026', 'CareerGuide', 'InReview', 'en', @AuthorId, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

    INSERT INTO [editorial].[ContentVersions] (ArticleId, VersionNumber, Title, Summary, Body, CreatedBy, IsCurrentVersion, WordCount, ReadingTimeMinutes)
    VALUES (@Article2Id, 1, 'Medical vs Biotechnology: Choosing the Right Career Path in 2026', 'An in-depth comparison of MBBS, BDS, B.Pharm, and Biotechnology career trajectories in India.', 'While MBBS offers direct clinical practice, Biotechnology and Bioinformatics are witnessing exponential growth in genomics, pharma R&D, and health-tech startups.', @AuthorId, 1, 420, 4);
END;

-- Ensure Import Jobs exist
IF NOT EXISTS (SELECT 1 FROM [import].[Jobs])
BEGIN
    DECLARE @JobAdminId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [identity].[Users] ORDER BY CreatedAt ASC);
    IF @JobAdminId IS NULL SET @JobAdminId = NEWID();

    INSERT INTO [import].[Jobs] (Id, SourceType, Status, TotalRecords, ImportedRecords, ErrorCount, CreatedBy, CreatedAt)
    VALUES 
    (NEWID(), 'All-India Entrance Exams (NTA / State Boards)', 'Completed', 58, 58, 0, @JobAdminId, SYSUTCDATETIME()),
    (NEWID(), 'Top 100 Emerging Careers & Indicative Salary Dataset 2026', 'Completed', 104, 104, 0, @JobAdminId, SYSUTCDATETIME());
END;
