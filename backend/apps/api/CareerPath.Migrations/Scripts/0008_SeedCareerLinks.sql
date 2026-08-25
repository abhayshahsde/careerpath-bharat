-- 0008: Seed career–skill, career–exam, career–course links

-- Software Engineer → Skills
INSERT INTO [catalog].[CareerSkills] (CareerId, SkillId, IsRequired)
SELECT c.Id, s.Id, 1
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Skills] s
WHERE c.Slug = 'software-engineer'
  AND s.Slug IN ('python','java','sql','cloud-computing','problem-solving','communication')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerSkills] cs
    WHERE cs.CareerId = c.Id AND cs.SkillId = s.Id
  );

INSERT INTO [catalog].[CareerSkills] (CareerId, SkillId, IsRequired)
SELECT c.Id, s.Id, 0
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Skills] s
WHERE c.Slug = 'software-engineer'
  AND s.Slug IN ('machine-learning','data-analysis','leadership')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerSkills] cs
    WHERE cs.CareerId = c.Id AND cs.SkillId = s.Id
  );

-- Medical Doctor → Skills
INSERT INTO [catalog].[CareerSkills] (CareerId, SkillId, IsRequired)
SELECT c.Id, s.Id, 1
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Skills] s
WHERE c.Slug = 'medical-doctor'
  AND s.Slug IN ('patient-care','critical-thinking','communication','research')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerSkills] cs
    WHERE cs.CareerId = c.Id AND cs.SkillId = s.Id
  );

-- IAS Officer → Skills
INSERT INTO [catalog].[CareerSkills] (CareerId, SkillId, IsRequired)
SELECT c.Id, s.Id, 1
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Skills] s
WHERE c.Slug = 'ias-officer'
  AND s.Slug IN ('public-admin','leadership','critical-thinking','communication')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerSkills] cs
    WHERE cs.CareerId = c.Id AND cs.SkillId = s.Id
  );

-- Software Engineer → Exams
INSERT INTO [catalog].[CareerExams] (CareerId, ExamId, IsRequired, SortOrder)
SELECT c.Id, e.Id, 0, ROW_NUMBER() OVER (ORDER BY e.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Exams] e
WHERE c.Slug = 'software-engineer'
  AND e.Slug IN ('jee-main','jee-adv','gate','cuet-ug')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerExams] ce
    WHERE ce.CareerId = c.Id AND ce.ExamId = e.Id
  );

-- Medical Doctor → Exams
INSERT INTO [catalog].[CareerExams] (CareerId, ExamId, IsRequired, SortOrder)
SELECT c.Id, e.Id, 1, ROW_NUMBER() OVER (ORDER BY e.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Exams] e
WHERE c.Slug = 'medical-doctor'
  AND e.Slug IN ('neet-ug')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerExams] ce
    WHERE ce.CareerId = c.Id AND ce.ExamId = e.Id
  );

-- IAS Officer → Exams
INSERT INTO [catalog].[CareerExams] (CareerId, ExamId, IsRequired, SortOrder)
SELECT c.Id, e.Id, 1, ROW_NUMBER() OVER (ORDER BY e.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Exams] e
WHERE c.Slug = 'ias-officer'
  AND e.Slug IN ('upsc-cse')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerExams] ce
    WHERE ce.CareerId = c.Id AND ce.ExamId = e.Id
  );

-- Software Engineer → Courses
INSERT INTO [catalog].[CareerCourses] (CareerId, CourseId, IsRequired, SortOrder)
SELECT c.Id, co.Id, 1, ROW_NUMBER() OVER (ORDER BY co.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Courses] co
WHERE c.Slug = 'software-engineer'
  AND co.Slug IN ('btech-cs','msc-cs','diploma-web-dev')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerCourses] cc
    WHERE cc.CareerId = c.Id AND cc.CourseId = co.Id
  );

-- Medical Doctor → Courses
INSERT INTO [catalog].[CareerCourses] (CareerId, CourseId, IsRequired, SortOrder)
SELECT c.Id, co.Id, 1, ROW_NUMBER() OVER (ORDER BY co.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Courses] co
WHERE c.Slug = 'medical-doctor'
  AND co.Slug IN ('mbbs')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerCourses] cc
    WHERE cc.CareerId = c.Id AND cc.CourseId = co.Id
  );

-- IAS Officer → Courses
INSERT INTO [catalog].[CareerCourses] (CareerId, CourseId, IsRequired, SortOrder)
SELECT c.Id, co.Id, 0, ROW_NUMBER() OVER (ORDER BY co.Name)
FROM [catalog].[Careers] c
CROSS JOIN [catalog].[Courses] co
WHERE c.Slug = 'ias-officer'
  AND co.Slug IN ('ba-economics','llb')
  AND NOT EXISTS (
    SELECT 1 FROM [catalog].[CareerCourses] cc
    WHERE cc.CareerId = c.Id AND cc.CourseId = co.Id
  );
