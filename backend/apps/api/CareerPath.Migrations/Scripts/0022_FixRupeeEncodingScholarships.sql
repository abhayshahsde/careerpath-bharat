-- 0022: Fix Rupee encoding in Scholarships and Careers default labels

-- 1. Update Scholarships AmountLabel with proper Unicode N prefix
UPDATE [catalog].[Scholarships]
SET AmountLabel = N'Up to ₹50,000/year'
WHERE Slug = 'nsp-central';

UPDATE [catalog].[Scholarships]
SET AmountLabel = N'₹80,000/year'
WHERE Slug = 'inspire-scholarship';

UPDATE [catalog].[Scholarships]
SET AmountLabel = N'₹2,500 – ₹3,000/month'
WHERE Slug = 'pm-scholarship';

UPDATE [catalog].[Scholarships]
SET AmountLabel = N'₹50,000/year + tuition'
WHERE Slug = 'aicte-pragati';

UPDATE [catalog].[Scholarships]
SET AmountLabel = N'Up to ₹20,000/year'
WHERE Slug = 'merit-cum-means';

-- 2. Correct Careers default English salary labels with proper Unicode N prefix
UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹6L – ₹40L per year'
WHERE Slug = 'software-engineer';

UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹8L – ₹30L per year'
WHERE Slug = 'medical-doctor';

UPDATE [catalog].[Careers]
SET SalaryRangeLabel = N'₹6L – ₹20L per year'
WHERE Slug = 'ias-officer';
