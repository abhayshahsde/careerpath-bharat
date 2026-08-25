-- 0003: Identity skeleton tables
-- Users, Roles, UserRoles, Permissions, RolePermissions, Sessions, RefreshTokens, LoginAttempts

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='Roles')
BEGIN
    CREATE TABLE [identity].[Roles] (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive    BIT NOT NULL DEFAULT 1,
        CreatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Roles_Name ON [identity].[Roles] (Name);

    INSERT INTO [identity].[Roles] (Name, Description) VALUES
        ('Student',       'Standard student user'),
        ('Guardian',      'Parent or guardian account'),
        ('Counselor',     'School or career counselor'),
        ('ContentEditor', 'Can create and edit catalog content'),
        ('Reviewer',      'Can review submitted content'),
        ('Admin',         'Platform administrator'),
        ('SuperAdmin',    'Unrestricted platform access'),
        ('Support',       'Customer support agent'),
        ('FinanceAdmin',  'Billing and subscription access'),
        ('SecurityAdmin', 'Security event and audit access');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='Permissions')
BEGIN
    CREATE TABLE [identity].[Permissions] (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        Module      NVARCHAR(100) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Permissions_Name ON [identity].[Permissions] (Name);

    INSERT INTO [identity].[Permissions] (Name, Module) VALUES
        ('Careers.View',       'Careers'),
        ('Careers.Edit',       'Careers'),
        ('Careers.Publish',    'Careers'),
        ('Careers.Archive',    'Careers'),
        ('Catalog.Edit',       'Catalog'),
        ('Catalog.Publish',    'Catalog'),
        ('Imports.Upload',     'Imports'),
        ('Imports.Approve',    'Imports'),
        ('Exports.Self',       'Exports'),
        ('Exports.AdminUsers', 'Exports'),
        ('Knowledge.Edit',     'Knowledge'),
        ('Knowledge.Publish',  'Knowledge'),
        ('AI.Chat',            'AI'),
        ('AI.PromptManage',    'AI'),
        ('Users.View',         'Users'),
        ('Users.Suspend',      'Users'),
        ('Audit.View',         'Audit'),
        ('System.Admin',       'System');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='Users')
BEGIN
    CREATE TABLE [identity].[Users] (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        Email           NVARCHAR(320) NOT NULL,
        PasswordHash    NVARCHAR(1000) NOT NULL,
        DisplayName     NVARCHAR(200) NULL,
        IsEmailVerified BIT NOT NULL DEFAULT 0,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Users_Email ON [identity].[Users] (Email);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='UserRoles')
BEGIN
    CREATE TABLE [identity].[UserRoles] (
        UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        RoleId      INT NOT NULL REFERENCES [identity].[Roles](Id),
        AssignedAt  DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        PRIMARY KEY (UserId, RoleId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='RolePermissions')
BEGIN
    CREATE TABLE [identity].[RolePermissions] (
        RoleId       INT NOT NULL REFERENCES [identity].[Roles](Id),
        PermissionId INT NOT NULL REFERENCES [identity].[Permissions](Id),
        PRIMARY KEY (RoleId, PermissionId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='Sessions')
BEGIN
    CREATE TABLE [identity].[Sessions] (
        Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        UserId      UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Users](Id),
        DeviceInfo  NVARCHAR(500) NULL,
        IpAddress   NVARCHAR(50) NULL,
        CreatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        LastSeenAt  DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        RevokedAt   DATETIMEOFFSET(7) NULL
    );
    CREATE INDEX IX_Sessions_UserId ON [identity].[Sessions] (UserId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='RefreshTokens')
BEGIN
    CREATE TABLE [identity].[RefreshTokens] (
        Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
        SessionId   UNIQUEIDENTIFIER NOT NULL REFERENCES [identity].[Sessions](Id),
        TokenHash   NVARCHAR(1000) NOT NULL,
        FamilyId    UNIQUEIDENTIFIER NOT NULL,
        UsedAt      DATETIMEOFFSET(7) NULL,
        ExpiresAt   DATETIMEOFFSET(7) NOT NULL,
        CreatedAt   DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON [identity].[RefreshTokens] (TokenHash);
    CREATE INDEX IX_RefreshTokens_FamilyId ON [identity].[RefreshTokens] (FamilyId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name='identity' AND t.name='LoginAttempts')
BEGIN
    CREATE TABLE [identity].[LoginAttempts] (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Email       NVARCHAR(320) NOT NULL,
        IpAddress   NVARCHAR(50) NULL,
        Succeeded   BIT NOT NULL,
        AttemptedAt DATETIMEOFFSET(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_LoginAttempts_Email ON [identity].[LoginAttempts] (Email, AttemptedAt);
END;
