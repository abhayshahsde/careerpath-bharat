-- 0004: Catalog skeleton tables
-- Careers, CareerTranslations, Categories

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Categories')
BEGIN
    CREATE TABLE [catalog].[Categories] (
        Id          NVARCHAR(100) NOT NULL PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        ParentId    NVARCHAR(100) NULL REFERENCES [catalog].[Categories](Id),
        SortOrder   INT NOT NULL DEFAULT 0,
        IsActive    BIT NOT NULL DEFAULT 1
    );

    INSERT INTO [catalog].[Categories] (Id, Name, SortOrder) VALUES
        ('engineering',    'Engineering & Technology', 1),
        ('medicine',       'Medicine & Healthcare',    2),
        ('law',            'Law & Legal Services',     3),
        ('business',       'Business & Management',    4),
        ('arts',           'Arts & Design',            5),
        ('education',      'Education & Teaching',     6),
        ('science',        'Science & Research',       7),
        ('government',     'Government & Civil Services', 8),
        ('media',          'Media & Communications',   9),
        ('sports',         'Sports & Fitness',         10);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Careers')
BEGIN
    CREATE TABLE [catalog].[Careers] (
        Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Slug                NVARCHAR(300) NOT NULL,
        CategoryId          NVARCHAR(100) NULL REFERENCES [catalog].[Categories](Id),
        Status              NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        IsFeatured          BIT NOT NULL DEFAULT 0,
        MinEducationYears   INT NOT NULL DEFAULT 0,
        MaxEducationYears   INT NOT NULL DEFAULT 0,
        SalaryRangeLabel    NVARCHAR(200) NULL,
        ImageUrl            NVARCHAR(1000) NULL,
        CreatedAt           DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        PublishedAt         DATETIMEOFFSET(7) NULL
    );
    CREATE UNIQUE INDEX UX_Careers_Slug ON [catalog].[Careers] (Slug);
    CREATE INDEX IX_Careers_Status_PublishedAt ON [catalog].[Careers] (Status, PublishedAt DESC);
    CREATE INDEX IX_Careers_CategoryId ON [catalog].[Careers] (CategoryId);

    -- Seed a few sample careers for local dev
    DECLARE @sw UNIQUEIDENTIFIER = NEWID();
    DECLARE @doc UNIQUEIDENTIFIER = NEWID();
    DECLARE @ias UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES
        (@sw,  'software-engineer',  'engineering', 'Published', 1, 4, 6, '₹6L – ₹40L per year', SYSUTCDATETIME()),
        (@doc, 'medical-doctor',     'medicine',    'Published', 1, 10, 14, '₹8L – ₹30L per year', SYSUTCDATETIME()),
        (@ias, 'ias-officer',        'government',  'Published', 1, 4, 6,  '₹6L – ₹20L per year', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='CareerTranslations')
BEGIN
    CREATE TABLE [catalog].[CareerTranslations] (
        CareerId    UNIQUEIDENTIFIER NOT NULL REFERENCES [catalog].[Careers](Id),
        Locale      NVARCHAR(10) NOT NULL,
        Title       NVARCHAR(500) NOT NULL,
        Summary     NVARCHAR(1000) NULL,
        Description NVARCHAR(MAX) NULL,
        Disclaimer  NVARCHAR(500) NULL,
        UpdatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        PRIMARY KEY (CareerId, Locale)
    );

    -- Seed translations for sample careers
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
    SELECT Id, 'en', 'Software Engineer',
        'Design, build and maintain software systems across web, mobile and infrastructure domains.',
        'Salary ranges are indicative and vary by employer, location and experience.'
    FROM [catalog].[Careers] WHERE Slug = 'software-engineer';

    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
    SELECT Id, 'hi', 'सॉफ्टवेयर इंजीनियर',
        'वेब, मोबाइल और इन्फ्रास्ट्रक्चर डोमेन में सॉफ्टवेयर सिस्टम डिज़ाइन, निर्माण और रखरखाव करें।',
        'वेतन सीमाएं सांकेतिक हैं और नियोक्ता, स्थान और अनुभव के अनुसार भिन्न होती हैं।'
    FROM [catalog].[Careers] WHERE Slug = 'software-engineer';

    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
    SELECT Id, 'en', 'Medical Doctor (MBBS/MD)',
        'Diagnose and treat patients across specialisations in hospitals, clinics and community settings.',
        'Admission eligibility depends on entrance exam results and regulations. We do not guarantee admission.'
    FROM [catalog].[Careers] WHERE Slug = 'medical-doctor';

    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
    SELECT Id, 'en', 'IAS Officer (Indian Administrative Service)',
        'Serve as a senior civil servant shaping public policy, administration and governance across India.',
        'Selection is through UPSC CSE. We do not guarantee selection or exam success.'
    FROM [catalog].[Careers] WHERE Slug = 'ias-officer';
END;
