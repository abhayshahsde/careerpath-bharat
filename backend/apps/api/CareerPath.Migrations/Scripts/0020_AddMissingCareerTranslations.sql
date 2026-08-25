-- 0020: Add Missing Career Translations and Fix Rupee Encoding in Salary Labels

-- 1. Fix Rupee symbol encoding in catalog.Careers
UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹6L – ₹40L प्रति वर्ष'
WHERE Slug = 'software-engineer';

UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹8L – ₹30L प्रति वर्ष'
WHERE Slug = 'medical-doctor';

UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹6L – ₹20L प्रति वर्ष'
WHERE Slug = 'ias-officer';

-- 2. Add Hindi translations for Medical Doctor
IF EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'medical-doctor')
BEGIN
    DECLARE @docId UNIQUEIDENTIFIER;
    SELECT @docId = Id FROM [catalog].[Careers] WHERE Slug = 'medical-doctor';

    IF NOT EXISTS (SELECT 1 FROM [catalog].[CareerTranslations] WHERE CareerId = @docId AND Locale = 'hi')
    BEGIN
        INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
        VALUES (@docId, 'hi', N'चिकित्सा चिकित्सक (MBBS/MD)',
            N'अस्पतालों, क्लीनिकों और सामुदायिक सेटिंग्स में विभिन्न विशिष्टताओं के रोगियों का निदान और उपचार करें।',
            N'प्रवेश पात्रता प्रवेश परीक्षा परिणामों और नियमों पर निर्भर करती है। हम प्रवेश की गारंटी नहीं देते हैं।');
    END;
END;

-- 3. Add Hindi translations for IAS Officer
IF EXISTS (SELECT 1 FROM [catalog].[Careers] WHERE Slug = 'ias-officer')
BEGIN
    DECLARE @iasId UNIQUEIDENTIFIER;
    SELECT @iasId = Id FROM [catalog].[Careers] WHERE Slug = 'ias-officer';

    IF NOT EXISTS (SELECT 1 FROM [catalog].[CareerTranslations] WHERE CareerId = @iasId AND Locale = 'hi')
    BEGIN
        INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary, Disclaimer)
        VALUES (@iasId, 'hi', N'आईएएस अधिकारी (भारतीय प्रशासनिक सेवा)',
            N'पूरे भारत में सार्वजनिक नीति, प्रशासन और शासन को आकार देने वाले एक वरिष्ठ सिविल सेवक के रूप में सेवा करें।',
            N'चयन यूपीएससी सीएसई के माध्यम से होता है। हम चयन या परीक्षा में सफलता की गारंटी नहीं देते हैं।');
    END;
END;
