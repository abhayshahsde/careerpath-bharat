-- 0005: Student profile tables

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='student' AND t.name='Profiles')
BEGIN
    CREATE TABLE [student].[Profiles] (
        Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId                  UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        DisplayName             NVARCHAR(200) NULL,
        AvatarUrl               NVARCHAR(1000) NULL,
        CurrentEducationLevel   NVARCHAR(100) NULL,
        StateOfResidence        NVARCHAR(100) NULL,
        PreferredLocale         NVARCHAR(10) NULL,
        IsOnboardingComplete    BIT NOT NULL DEFAULT 0,
        CreatedAt               DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Profiles_UserId ON [student].[Profiles] (UserId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='student' AND t.name='SavedItems')
BEGIN
    CREATE TABLE [student].[SavedItems] (
        Id       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId   UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        ItemType NVARCHAR(50) NOT NULL,   -- Career, Course, Scholarship
        ItemId   UNIQUEIDENTIFIER NOT NULL,
        SavedAt  DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_SavedItems_UserItemTypeItem ON [student].[SavedItems] (UserId, ItemType, ItemId);
    CREATE INDEX IX_SavedItems_UserId_ItemType ON [student].[SavedItems] (UserId, ItemType, SavedAt DESC);
END;
