-- 0007: Extended catalog tables
-- Courses, Exams, Skills, Scholarships, CareerSkills, CareerExams

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Skills')
BEGIN
    CREATE TABLE [catalog].[Skills] (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Slug        NVARCHAR(200) NOT NULL,
        Category    NVARCHAR(100) NULL,   -- e.g. 'Technical', 'Soft', 'Domain'
        IsActive    BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Skills_Slug ON [catalog].[Skills] (Slug);

    INSERT INTO [catalog].[Skills] (Name, Slug, Category) VALUES
        ('Python Programming',     'python',             'Technical'),
        ('Data Analysis',          'data-analysis',      'Technical'),
        ('Communication',          'communication',      'Soft'),
        ('Problem Solving',        'problem-solving',    'Soft'),
        ('Leadership',             'leadership',         'Soft'),
        ('Machine Learning',       'machine-learning',   'Technical'),
        ('SQL',                    'sql',                'Technical'),
        ('Critical Thinking',      'critical-thinking',  'Soft'),
        ('Research',               'research',           'Domain'),
        ('Patient Care',           'patient-care',       'Domain'),
        ('Legal Drafting',         'legal-drafting',     'Domain'),
        ('Financial Analysis',     'financial-analysis', 'Domain'),
        ('Public Administration',  'public-admin',       'Domain'),
        ('Java',                   'java',               'Technical'),
        ('Cloud Computing',        'cloud-computing',    'Technical');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Exams')
BEGIN
    CREATE TABLE [catalog].[Exams] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Slug            NVARCHAR(200) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        FullName        NVARCHAR(500) NULL,
        ConductingBody  NVARCHAR(300) NULL,
        Level           NVARCHAR(100) NULL,   -- 'National', 'State', 'University', 'International'
        Frequency       NVARCHAR(100) NULL,   -- 'Annual', 'Bi-Annual', 'Monthly'
        Description     NVARCHAR(2000) NULL,
        OfficialUrl     NVARCHAR(1000) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Exams_Slug ON [catalog].[Exams] (Slug);

    INSERT INTO [catalog].[Exams] (Slug, Name, FullName, ConductingBody, Level, Frequency) VALUES
        ('jee-main',    'JEE Main',    'Joint Entrance Examination – Main',          'NTA',    'National', 'Bi-Annual'),
        ('jee-adv',     'JEE Advanced','Joint Entrance Examination – Advanced',      'IIT',    'National', 'Annual'),
        ('neet-ug',     'NEET UG',     'National Eligibility cum Entrance Test UG',  'NTA',    'National', 'Annual'),
        ('upsc-cse',    'UPSC CSE',    'Union Public Service Commission Civil Services Exam', 'UPSC', 'National', 'Annual'),
        ('cat',         'CAT',         'Common Admission Test',                      'IIMs',   'National', 'Annual'),
        ('gate',        'GATE',        'Graduate Aptitude Test in Engineering',      'IIT/IISC','National','Annual'),
        ('clat',        'CLAT',        'Common Law Admission Test',                  'NLU Consortium','National','Annual'),
        ('nda',         'NDA',         'National Defence Academy',                   'UPSC',   'National', 'Bi-Annual'),
        ('cuet-ug',     'CUET UG',     'Common University Entrance Test',            'NTA',    'National', 'Annual'),
        ('ssc-cgl',     'SSC CGL',     'Staff Selection Commission Combined Graduate Level','SSC','National','Annual');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Courses')
BEGIN
    CREATE TABLE [catalog].[Courses] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Slug            NVARCHAR(200) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        ShortName       NVARCHAR(100) NULL,
        DegreeLevel     NVARCHAR(100) NOT NULL,  -- 'Undergraduate','Postgraduate','Diploma','Certificate','Doctoral'
        DurationYears   DECIMAL(4,1) NOT NULL DEFAULT 3,
        CategoryId      NVARCHAR(100) NULL REFERENCES [catalog].[Categories](Id),
        Description     NVARCHAR(2000) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Courses_Slug ON [catalog].[Courses] (Slug);

    INSERT INTO [catalog].[Courses] (Slug, Name, ShortName, DegreeLevel, DurationYears, CategoryId) VALUES
        ('btech-cs',        'Bachelor of Technology – Computer Science', 'B.Tech CS',    'Undergraduate', 4.0, 'engineering'),
        ('mbbs',            'Bachelor of Medicine and Surgery',           'MBBS',         'Undergraduate', 5.5, 'medicine'),
        ('llb',             'Bachelor of Laws',                           'LLB',          'Undergraduate', 3.0, 'law'),
        ('mba',             'Master of Business Administration',          'MBA',          'Postgraduate',  2.0, 'business'),
        ('msc-cs',          'Master of Science – Computer Science',       'M.Sc CS',      'Postgraduate',  2.0, 'engineering'),
        ('bsc-nursing',     'Bachelor of Science – Nursing',              'B.Sc Nursing', 'Undergraduate', 4.0, 'medicine'),
        ('ba-economics',    'Bachelor of Arts – Economics',               'BA Economics', 'Undergraduate', 3.0, 'business'),
        ('barch',           'Bachelor of Architecture',                   'B.Arch',       'Undergraduate', 5.0, 'engineering'),
        ('phd-cs',          'Doctor of Philosophy – Computer Science',    'Ph.D CS',      'Doctoral',      4.0, 'engineering'),
        ('diploma-web-dev', 'Diploma in Web Development',                 'Web Dev',      'Diploma',       1.0, 'engineering');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='Scholarships')
BEGIN
    CREATE TABLE [catalog].[Scholarships] (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Slug            NVARCHAR(200) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        ProviderName    NVARCHAR(300) NOT NULL,
        Level           NVARCHAR(100) NULL,   -- 'Undergraduate','Postgraduate','All'
        AmountLabel     NVARCHAR(200) NULL,
        EligibilitySummary NVARCHAR(1000) NULL,
        OfficialUrl     NVARCHAR(1000) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        Disclaimer      NVARCHAR(500) NULL
    );
    CREATE UNIQUE INDEX UX_Scholarships_Slug ON [catalog].[Scholarships] (Slug);

    INSERT INTO [catalog].[Scholarships] (Slug, Name, ProviderName, Level, AmountLabel, EligibilitySummary, Disclaimer) VALUES
        ('nsp-central', 'National Scholarship Portal', 'Ministry of Education', 'All',
            'Up to ₹50,000/year', 'Open to students from economically weaker sections',
            'Eligibility varies. Visit NSP portal for current guidelines.'),
        ('inspire-scholarship', 'INSPIRE Scholarship', 'DST (Dept. of Science & Technology)', 'Undergraduate',
            '₹80,000/year', 'Top 1% in Class 12 board exams; pursuing science at UG level',
            'Amounts are indicative. Check DST website for current rates.'),
        ('pm-scholarship', 'PM Scholarship Scheme', 'Ministry of Home Affairs', 'Undergraduate',
            '₹2,500 – ₹3,000/month', 'Wards of ex-servicemen or para-military personnel',
            'For professional degree programmes only.'),
        ('aicte-pragati', 'AICTE Pragati Scholarship (Girls)', 'AICTE', 'Undergraduate',
            '₹50,000/year + tuition', 'Girl students in AICTE-approved technical institutions',
            'One scholarship per family. Limited seats.'),
        ('merit-cum-means', 'Merit-cum-Means Scholarship (Minorities)', 'Ministry of Minority Affairs', 'All',
            'Up to ₹20,000/year', 'Minority community students with family income ≤ ₹2.5L/year',
            'Amounts subject to revision. Verify at NSP.');
END;

-- Linking tables: CareerSkills, CareerExams, CareerCourses

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='CareerSkills')
BEGIN
    CREATE TABLE [catalog].[CareerSkills] (
        CareerId    UNIQUEIDENTIFIER NOT NULL REFERENCES [catalog].[Careers](Id),
        SkillId     INT NOT NULL REFERENCES [catalog].[Skills](Id),
        IsRequired  BIT NOT NULL DEFAULT 1,
        PRIMARY KEY (CareerId, SkillId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='CareerExams')
BEGIN
    CREATE TABLE [catalog].[CareerExams] (
        CareerId    UNIQUEIDENTIFIER NOT NULL REFERENCES [catalog].[Careers](Id),
        ExamId      INT NOT NULL REFERENCES [catalog].[Exams](Id),
        IsRequired  BIT NOT NULL DEFAULT 0,
        SortOrder   INT NOT NULL DEFAULT 0,
        PRIMARY KEY (CareerId, ExamId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='catalog' AND t.name='CareerCourses')
BEGIN
    CREATE TABLE [catalog].[CareerCourses] (
        CareerId    UNIQUEIDENTIFIER NOT NULL REFERENCES [catalog].[Careers](Id),
        CourseId    INT NOT NULL REFERENCES [catalog].[Courses](Id),
        IsRequired  BIT NOT NULL DEFAULT 0,
        SortOrder   INT NOT NULL DEFAULT 0,
        PRIMARY KEY (CareerId, CourseId)
    );
END;
