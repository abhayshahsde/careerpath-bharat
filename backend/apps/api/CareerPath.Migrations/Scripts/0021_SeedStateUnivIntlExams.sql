-- 0021: Seed State, University, and International Level Entrance Exams

-- 1. Insert sample exams into catalog.Exams
IF NOT EXISTS (SELECT 1 FROM [catalog].[Exams] WHERE Slug = 'mhcet')
BEGIN
    INSERT INTO [catalog].[Exams] (Slug, Name, FullName, ConductingBody, Level, Frequency)
    VALUES ('mhcet', 'MHT CET', 'Maharashtra Common Entrance Test', 'State CET Cell', 'State', 'Annual');
END;

IF NOT EXISTS (SELECT 1 FROM [catalog].[Exams] WHERE Slug = 'bitsat')
BEGIN
    INSERT INTO [catalog].[Exams] (Slug, Name, FullName, ConductingBody, Level, Frequency)
    VALUES ('bitsat', 'BITSAT', 'BITS Pilani Admission Test', 'BITS Pilani', 'University', 'Annual');
END;

IF NOT EXISTS (SELECT 1 FROM [catalog].[Exams] WHERE Slug = 'sat')
BEGIN
    INSERT INTO [catalog].[Exams] (Slug, Name, FullName, ConductingBody, Level, Frequency)
    VALUES ('sat', 'SAT', 'Scholastic Assessment Test', 'College Board', 'International', 'Monthly');
END;

-- 2. Seed English translations for new exams
INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'en', 'MHT CET', 'Maharashtra Common Entrance Test', 'State-level entrance exam for engineering and pharmacy courses in Maharashtra.'
FROM [catalog].[Exams] WHERE Slug = 'mhcet' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'en');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'en', 'BITSAT', 'BITS Pilani Admission Test', 'University-level online test for admission to integrated first-degree programmes of BITS Pilani.'
FROM [catalog].[Exams] WHERE Slug = 'bitsat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'en');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'en', 'SAT', 'Scholastic Assessment Test', 'International standardized test widely used for college admissions in the United States and other countries.'
FROM [catalog].[Exams] WHERE Slug = 'sat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'en');

-- 3. Seed Hindi translations for new exams
INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'एमएचटी सीईटी (MHT CET)', N'महाराष्ट्र कॉमन एंट्रेंस टेस्ट', N'महाराष्ट्र में इंजीनियरिंग और फार्मेसी पाठ्यक्रमों के लिए राज्य स्तरीय प्रवेश परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'mhcet' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'बिटसैट (BITSAT)', N'बिट्स पिलानी प्रवेश परीक्षा', N'बिट्स पिलानी के एकीकृत प्रथम-डिग्री कार्यक्रमों में प्रवेश के लिए विश्वविद्यालय स्तरीय ऑनलाइन परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'bitsat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'सैट (SAT)', N'स्कोलास्टिक एसेसमेंट टेस्ट', N'संयुक्त राज्य अमेरिका और अन्य देशों में कॉलेज प्रवेश के लिए व्यापक रूप से उपयोग की जाने वाली अंतर्राष्ट्रीय मानकीकृत परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'sat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');
