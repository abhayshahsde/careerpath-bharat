-- 0023: Fix Hindi Translation Encoding in CareerTranslations for Software Engineer

-- Update Devanagari translation texts with proper Unicode N prefix
UPDATE [catalog].[CareerTranslations]
SET Title = N'सॉफ्टवेयर इंजीनियर',
    Summary = N'वेब, मोबाइल और इन्फ्रास्ट्रक्चर डोमेन में सॉफ्टवेयर सिस्टम डिज़ाइन, निर्माण और रखरखाव करें।',
    Disclaimer = N'वेतन सीमाएं सांकेतिक हैं और नियोक्ता, स्थान और अनुभव के अनुसार भिन्न होती हैं।'
WHERE CareerId IN (SELECT Id FROM [catalog].[Careers] WHERE Slug = 'software-engineer')
  AND Locale = 'hi';
