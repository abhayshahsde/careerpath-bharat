-- 0001: Create all domain schemas
-- Applied once. DO NOT modify after application.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'identity')   EXEC('CREATE SCHEMA [identity]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'student')    EXEC('CREATE SCHEMA [student]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'catalog')    EXEC('CREATE SCHEMA [catalog]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'editorial')  EXEC('CREATE SCHEMA [editorial]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'recommendation') EXEC('CREATE SCHEMA [recommendation]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'roadmap')    EXEC('CREATE SCHEMA [roadmap]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ai')         EXEC('CREATE SCHEMA [ai]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'knowledge')  EXEC('CREATE SCHEMA [knowledge]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'import')     EXEC('CREATE SCHEMA [import]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'export')     EXEC('CREATE SCHEMA [export]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'notification') EXEC('CREATE SCHEMA [notification]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')    EXEC('CREATE SCHEMA [billing]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'analytics')  EXEC('CREATE SCHEMA [analytics]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'audit')      EXEC('CREATE SCHEMA [audit]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'system')     EXEC('CREATE SCHEMA [system]');
