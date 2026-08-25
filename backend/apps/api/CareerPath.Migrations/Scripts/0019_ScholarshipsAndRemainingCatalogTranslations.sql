-- 0019: Scholarships and Remaining Catalog Translations

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'ScholarshipTranslations')
BEGIN
    CREATE TABLE [catalog].[ScholarshipTranslations] (
        ScholarshipId   INT NOT NULL REFERENCES [catalog].[Scholarships](Id) ON DELETE CASCADE,
        Locale          NVARCHAR(10) NOT NULL,
        Name            NVARCHAR(500) NOT NULL,
        ProviderName    NVARCHAR(300) NOT NULL,
        EligibilitySummary NVARCHAR(1000) NULL,
        Disclaimer      NVARCHAR(500) NULL,
        PRIMARY KEY (ScholarshipId, Locale)
    );
    CREATE INDEX IX_ScholarshipTranslations_ScholarshipId ON [catalog].[ScholarshipTranslations] (ScholarshipId);
END;

-- Seed 'en' translations from existing scholarships table
INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'en', Name, ProviderName, EligibilitySummary, Disclaimer FROM [catalog].[Scholarships]
WHERE Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'en');

-- Seed 'hi' translations for all Scholarships
INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'hi', N'राष्ट्रीय छात्रवृत्ति पोर्टल (NSP)', N'शिक्षा मंत्रालय', N'आर्थिक रूप से कमजोर वर्गों के छात्रों के लिए खुला है', N'पात्रता भिन्न हो सकती है। वर्तमान दिशानिर्देशों के लिए एनएसपी पोर्टल पर जाएं।'
FROM [catalog].[Scholarships] WHERE Slug = 'nsp-central'
AND Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'hi', N'इंस्पायर छात्रवृत्ति', N'डीएसटी (विज्ञान और प्रौद्योगिकी विभाग)', N'कक्षा 12 बोर्ड परीक्षाओं में शीर्ष 1%; स्नातक स्तर पर विज्ञान की पढ़ाई करने वाले छात्रों के लिए', N'राशि सांकेतिक है। वर्तमान दरों के लिए डीएसटी वेबसाइट देखें।'
FROM [catalog].[Scholarships] WHERE Slug = 'inspire-scholarship'
AND Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'hi', N'प्रधानमंत्री छात्रवृत्ति योजना', N'गृह मंत्रालय', N'भूतपूर्व सैनिकों या अर्धसैनिक बलों के आश्रितों के लिए', N'केवल व्यावसायिक डिग्री कार्यक्रमों के लिए।'
FROM [catalog].[Scholarships] WHERE Slug = 'pm-scholarship'
AND Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'hi', N'एआईसीटीई प्रगति छात्रवृत्ति (छात्राएं)', N'एआईसीटीई', N'एआईसीटीई-अनुमोदित तकनीकी संस्थानों में पढ़ने वाली छात्राएं', N'प्रति परिवार एक छात्रवृत्ति। सीमित सीटें।'
FROM [catalog].[Scholarships] WHERE Slug = 'aicte-pragati'
AND Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ScholarshipTranslations] (ScholarshipId, Locale, Name, ProviderName, EligibilitySummary, Disclaimer)
SELECT Id, 'hi', N'मेरिट-कम-मीन्स छात्रवृत्ति (अल्पसंख्यक)', N'अल्पसंख्यक कार्य मंत्रालय', N'अल्पसंख्यक समुदाय के छात्र जिनकी पारिवारिक आय ≤ ₹2.5 लाख/वर्ष है', N'राशि संशोधन के अधीन है। एनएसपी पर सत्यापित करें।'
FROM [catalog].[Scholarships] WHERE Slug = 'merit-cum-means'
AND Id NOT IN (SELECT ScholarshipId FROM [catalog].[ScholarshipTranslations] WHERE Locale = 'hi');


-- Seed 'hi' translations for remaining Exams
INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'जेईई एडवांस्ड', N'संयुक्त प्रवेश परीक्षा – एडवांस्ड', N'आईआईटी में प्रवेश के लिए जेईई मेन उत्तीर्ण करने वाले छात्रों के लिए प्रवेश परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'jee-adv' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'यूपीएससी सीएसई', N'संघ लोक सेवा आयोग सिविल सेवा परीक्षा', N'भारत में प्रशासनिक और पुलिस सेवाओं (IAS, IPS, IFS) के लिए राष्ट्रीय परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'upsc-cse' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'कैट', N'कॉमन एडमिशन टेस्ट', N'आईआईएम और अन्य प्रतिष्ठित बिजनेस स्कूलों में एमबीए प्रवेश के लिए प्रवेश परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'cat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'गेट', N'इंजीनियरिंग में ग्रेजुएट एप्टीट्यूड टेस्ट', N'एम.टेक और सार्वजनिक क्षेत्र के उपक्रमों (PSUs) में नौकरियों के लिए परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'gate' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'क्लैट', N'कॉमन लॉ एडमिशन टेस्ट', N'भारत के राष्ट्रीय कानून विश्वविद्यालयों में कानून कार्यक्रमों (LLB, LLM) के लिए परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'clat' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'एनडीए', N'राष्ट्रीय रक्षा अकादमी परीक्षा', N'भारतीय सशस्त्र बलों (सेना, नौसेना, वायु सेना) में अधिकारियों के चयन के लिए परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'nda' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'सीयूईटी यूजी', N'कॉमन यूनिवर्सिटी एंट्रेंस टेस्ट स्नातक', N'केंद्रीय विश्वविद्यालयों में स्नातक कार्यक्रमों में प्रवेश के लिए राष्ट्रीय स्तर की परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'cuet-ug' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[ExamTranslations] (ExamId, Locale, Name, FullName, Description)
SELECT Id, 'hi', N'एसएससी सीजीएल', N'कर्मचारी चयन आयोग संयुक्त स्नातक स्तरीय परीक्षा', N'भारत सरकार के मंत्रालयों और विभागों में विभिन्न पदों पर भर्ती के लिए परीक्षा।'
FROM [catalog].[Exams] WHERE Slug = 'ssc-cgl' AND Id NOT IN (SELECT ExamId FROM [catalog].[ExamTranslations] WHERE Locale = 'hi');


-- Seed 'hi' translations for remaining Courses
INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'कानून स्नातक (एलएलबी)', N'व्यावसायिक कानूनी अभ्यास और न्यायशास्त्र पर केंद्रित 3-वर्षीय स्नातक कानून कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'llb' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'मास्टर ऑफ बिजनेस एडमिनिस्ट्रेशन (एमबीए)', N'संगठनात्मक नेतृत्व, वित्त और विपणन पर केंद्रित 2-वर्षीय स्नातकोत्तर व्यावसायिक कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'mba' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'कंप्यूटर विज्ञान में मास्टर ऑफ साइंस (M.Sc CS)', N'उन्नत कंप्यूटिंग, सॉफ्टवेयर इंजीनियरिंग और एल्गोरिदम विकास पर 2-वर्षीय वैज्ञानिक स्नातकोत्तर कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'msc-cs' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'नर्सिंग में विज्ञान स्नातक (B.Sc Nursing)', N'स्वास्थ्य देखभाल प्रबंधन, रोगी देखभाल और व्यावहारिक नैदानिक प्रशिक्षण पर 4-वर्षीय स्नातक नर्सिंग कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'bsc-nursing' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'कला स्नातक – अर्थशास्त्र (BA Economics)', N'आर्थिक सिद्धांतों, वित्तीय प्रणालियों और डेटा विश्लेषण पर केंद्रित 3-वर्षीय स्नातक कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'ba-economics' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'वास्तुकला स्नातक (B.Arch)', N'भवन डिजाइन, शहरी नियोजन और संरचनात्मक इंजीनियरिंग पर केंद्रित 5-वर्षीय व्यावसायिक स्नातक वास्तुकला कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'barch' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'कंप्यूटर विज्ञान में डॉक्टर ऑफ फिलॉसफी (Ph.D CS)', N'मूल शोध, मशीन लर्निंग और कम्प्यूटेशनल सिद्धांत पर केंद्रित 4-वर्षीय डॉक्टरेट शोध कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'phd-cs' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');

INSERT INTO [catalog].[CourseTranslations] (CourseId, Locale, Name, Description)
SELECT Id, 'hi', N'वेब डेवलपमेंट में डिप्लोमा (Diploma-Web-Dev)', N'फ्रंट-एंड, बैक-एंड प्रोग्रामिंग और व्यावहारिक कोडिंग पर ध्यान केंद्रित करने वाला 1-वर्षीय डिप्लोमा कार्यक्रम।'
FROM [catalog].[Courses] WHERE Slug = 'diploma-web-dev' AND Id NOT IN (SELECT CourseId FROM [catalog].[CourseTranslations] WHERE Locale = 'hi');
