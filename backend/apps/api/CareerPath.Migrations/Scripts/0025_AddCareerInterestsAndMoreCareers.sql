-- 0025: Create student.CareerInterests table and seed more careers across all categories

-- 1. Create CareerInterests table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='student' AND t.name='CareerInterests')
BEGIN
    CREATE TABLE [student].[CareerInterests] (
        Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        CategoryId  NVARCHAR(100) NOT NULL REFERENCES [catalog].[Categories](Id),
        Rank        INT NOT NULL DEFAULT 0,
        CreatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_CareerInterests_UserId ON [student].[CareerInterests] (UserId);
END;

-- 2. Media careers
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'journalist')
BEGIN
    DECLARE @journalist UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@journalist, 'journalist', 'media', 'Published', 1, 3, 5, N'3L - 15L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@journalist, 'en', 'Journalist / News Reporter', N'Report, investigate and communicate news stories across print, digital and broadcast media.'),
    (@journalist, 'hi', N'पत्रकार / समाचार रिपोर्टर', N'प्रिंट, डिजिटल और प्रसारण मीडिया में समाचार कहानियों की रिपोर्ट करें।');
END;

IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'content-creator')
BEGIN
    DECLARE @content UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@content, 'content-creator', 'media', 'Published', 1, 3, 5, N'2L - 20L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@content, 'en', 'Digital Content Creator / YouTuber', N'Create engaging content for digital platforms.'),
    (@content, 'hi', N'डिजिटल कंटेंट क्रिएटर', N'डिजिटल प्लेटफॉर्म के लिए आकर्षक सामग्री बनाएं।');
END;

-- Arts career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'graphic-designer')
BEGIN
    DECLARE @gd UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@gd, 'graphic-designer', 'arts', 'Published', 1, 3, 5, N'3L - 18L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@gd, 'en', 'Graphic Designer', N'Create visual concepts for branding, advertising, and digital media.'),
    (@gd, 'hi', N'ग्राफिक डिजाइनर', N'ब्रांडिंग और विज्ञापन के लिए दृश्य अवधारणाएं बनाएं।');
END;

-- Business career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'chartered-accountant')
BEGIN
    DECLARE @ca UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@ca, 'chartered-accountant', 'business', 'Published', 1, 5, 7, N'7L - 30L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@ca, 'en', 'Chartered Accountant (CA)', N'Provide financial advice, audit accounts and manage tax compliance.'),
    (@ca, 'hi', N'चार्टर्ड अकाउंटेंट (CA)', N'वित्तीय सलाह प्रदान करें और कर अनुपालन प्रबंधित करें।');
END;

-- Law career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'lawyer')
BEGIN
    DECLARE @lw UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@lw, 'lawyer', 'law', 'Published', 1, 5, 7, N'4L - 50L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@lw, 'en', 'Lawyer / Advocate', N'Represent clients in legal matters and provide expert legal advice.'),
    (@lw, 'hi', N'वकील / अधिवक्ता', N'कानूनी मामलों में ग्राहकों का प्रतिनिधित्व करें।');
END;

-- Education career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'school-teacher')
BEGIN
    DECLARE @teacher UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@teacher, 'school-teacher', 'education', 'Published', 1, 4, 6, N'3L - 10L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@teacher, 'en', 'School Teacher', N'Educate students at primary or secondary school level across various subjects.'),
    (@teacher, 'hi', N'स्कूल शिक्षक', N'विभिन्न विषयों में छात्रों को शिक्षित करें।');
END;

-- Science career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'research-scientist')
BEGIN
    DECLARE @sci UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@sci, 'research-scientist', 'science', 'Published', 1, 7, 10, N'5L - 25L per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@sci, 'en', 'Research Scientist', N'Conduct experiments and publish findings in biology, chemistry or physics.'),
    (@sci, 'hi', N'अनुसंधान वैज्ञानिक', N'जीव विज्ञान या रसायन विज्ञान में प्रयोग करें और निष्कर्ष प्रकाशित करें।');
END;

-- Sports career
IF NOT EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'professional-athlete')
BEGIN
    DECLARE @sp UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, Status, IsFeatured, MinEducationYears, MaxEducationYears, SalaryRangeLabel, PublishedAt)
    VALUES (@sp, 'professional-athlete', 'sports', 'Published', 1, 0, 3, N'2L - 1Cr+ per year', SYSUTCDATETIME());
    INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary) VALUES
    (@sp, 'en', 'Professional Athlete / Sports Person', N'Compete at national or international level in cricket, athletics, badminton or football.'),
    (@sp, 'hi', N'पेशेवर एथलीट / खिलाड़ी', N'क्रिकेट, एथलेटिक्स जैसे खेलों में राष्ट्रीय स्तर पर प्रतिस्पर्धा करें।');
END;
