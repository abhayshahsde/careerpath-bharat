-- 0018: Course and Exam Translations

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'CourseTranslations')
BEGIN
    CREATE TABLE [catalog].[CourseTranslations] (
        CourseId        INT NOT NULL REFERENCES [catalog].[Courses](Id) ON DELETE CASCADE,
        Locale          NVARCHAR(10) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        Description     NVARCHAR(2000) NULL,
        PRIMARY KEY (CourseId, Locale)
    );
    CREATE INDEX IX_CourseTranslations_CourseId ON [catalog].[CourseTranslations] (CourseId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'ExamTranslations')
BEGIN
    CREATE TABLE [catalog].[ExamTranslations] (
        ExamId          INT NOT NULL REFERENCES [catalog].[Exams](Id) ON DELETE CASCADE,
        Locale          NVARCHAR(10) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        FullName        NVARCHAR(500) NULL,
        Description     NVARCHAR(2000) NULL,
        PRIMARY KEY (ExamId, Locale)
    );
    CREATE INDEX IX_ExamTranslations_ExamId ON [catalog].[ExamTranslations] (ExamId);
END;

-- Seed initial 'en' translations from existing main tables
INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'en', Name, Description FROM [catalog].[Courses]
WHERE Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'en');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'en', Name, FullName, Description FROM [catalog].[Exams]
WHERE Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'en');

-- Seed 'hi' translations for major courses
INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'प्रौद्योगिकी स्नातक – कंप्यूटर विज्ञान', N'सॉफ्टवेयर इंजीनियरिंग, एल्गोरिदम और कंप्यूटिंग प्रणालियों पर केंद्रित 4-वर्षीय स्नातक कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'btech-cs'
AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'चिकित्सा और सर्जरी स्नातक (एमबीबीएस)', N'व्यावसायिक नैदानिक अभ्यास पर केंद्रित 5.5-वर्षीय चिकित्सा स्नातक कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'mbbs'
AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

-- Seed 'hi' translations for major exams
INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'जेईई मेन', N'संयुक्त प्रवेश परीक्षा – मुख्य', N'भारत के प्रतिष्ठित इंजीनियरिंग संस्थानों में प्रवेश के लिए राष्ट्रीय स्तर की प्रवेश परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'jee-main'
AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'नीट यूजी', N'राष्ट्रीय पात्रता सह प्रवेश परीक्षा स्नातक', N'मेडिकल कॉलेजों में प्रवेश के लिए भारत की राष्ट्रीय प्रवेश परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'neet-ug'
AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');
