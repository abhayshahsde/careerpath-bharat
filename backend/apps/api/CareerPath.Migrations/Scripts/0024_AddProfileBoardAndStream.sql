-- 0024: Add SchoolBoard and StreamOrSubjects columns to student.Profiles
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'student' AND t.name = 'Profiles' AND c.name = 'SchoolBoard'
)
BEGIN
    ALTER TABLE [student].[Profiles] ADD SchoolBoard NVARCHAR(150) NULL;
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'student' AND t.name = 'Profiles' AND c.name = 'StreamOrSubjects'
)
BEGIN
    ALTER TABLE [student].[Profiles] ADD StreamOrSubjects NVARCHAR(150) NULL;
END;
