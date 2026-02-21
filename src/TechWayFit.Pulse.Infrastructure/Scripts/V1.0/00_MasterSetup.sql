-- =============================================
-- TechWayFit Pulse - Master Setup Script
-- Version 1.0
-- =============================================
-- Description: Executes all setup scripts in order
-- Usage: Run this script against your SQL Server database
-- Prerequisites: 
--   - SQL Server 2016 or later
--   - Database already created (or create it first)
--   - Appropriate permissions (CREATE SCHEMA, CREATE TABLE, CREATE INDEX)
-- =============================================

-- Set database (update this to your database name)
USE [TechWayFitPulse]
GO

PRINT '========================================';
PRINT 'TechWayFit Pulse Database Setup v1.0';
PRINT '========================================';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
GO

-- =============================================
-- Step 1: Create Schema
-- =============================================
PRINT 'Step 1: Creating schema...';
GO

-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pulse')
BEGIN
    EXEC('CREATE SCHEMA pulse');
    PRINT 'Schema [pulse] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [pulse] already exists';
END
GO

PRINT '';
GO

-- =============================================
-- Step 2: Create Tables
-- =============================================
PRINT 'Step 2: Creating tables...';
GO

-- FacilitatorUsers Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[FacilitatorUsers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[FacilitatorUsers] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
      [Email] NVARCHAR(256) NOT NULL,
        [DisplayName] NVARCHAR(120) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [LastLoginAt] DATETIMEOFFSET NULL
    );
    PRINT 'Table [pulse].[FacilitatorUsers] created';
END
GO

-- FacilitatorUserData Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[FacilitatorUserData]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[FacilitatorUserData] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
  [FacilitatorUserId] UNIQUEIDENTIFIER NOT NULL,
        [Key] NVARCHAR(200) NOT NULL,
        [Value] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[FacilitatorUserData] created';
END
GO

-- LoginOtps Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[LoginOtps]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[LoginOtps] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Email] NVARCHAR(256) NOT NULL,
  [OtpCode] NVARCHAR(10) NOT NULL,
     [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ExpiresAt] DATETIMEOFFSET NOT NULL,
        [IsUsed] BIT NOT NULL DEFAULT 0,
   [UsedAt] DATETIMEOFFSET NULL
    );
    PRINT 'Table [pulse].[LoginOtps] created';
END
GO

-- SessionGroups Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[SessionGroups]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[SessionGroups] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
 [Name] NVARCHAR(200) NOT NULL,
 [Description] NVARCHAR(1000) NULL,
        [Level] INT NOT NULL,
     [ParentGroupId] UNIQUEIDENTIFIER NULL,
     [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
 [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
 [FacilitatorUserId] UNIQUEIDENTIFIER NULL,
      [Icon] NVARCHAR(50) NOT NULL DEFAULT 'folder',
        [Color] NVARCHAR(20) NULL,
        [IsDefault] BIT NOT NULL DEFAULT 0
    );
    PRINT 'Table [pulse].[SessionGroups] created';
END
GO

-- SessionTemplates Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[SessionTemplates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[SessionTemplates] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name] NVARCHAR(200) NOT NULL,
  [Description] NVARCHAR(500) NOT NULL,
      [Category] INT NOT NULL,
  [IconEmoji] NVARCHAR(10) NOT NULL,
     [ConfigJson] NVARCHAR(MAX) NOT NULL,
        [IsSystemTemplate] BIT NOT NULL DEFAULT 0,
        [CreatedByUserId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[SessionTemplates] created';
END
GO

-- Sessions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[Sessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[Sessions] (
[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Code] NVARCHAR(32) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Goal] NVARCHAR(MAX) NULL,
     [ContextJson] NVARCHAR(MAX) NULL,
        [SettingsJson] NVARCHAR(MAX) NOT NULL,
        [JoinFormSchemaJson] NVARCHAR(MAX) NOT NULL,
     [Status] INT NOT NULL DEFAULT 0,
    [CurrentActivityId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
      [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ExpiresAt] DATETIMEOFFSET NOT NULL,
        [FacilitatorUserId] UNIQUEIDENTIFIER NULL,
        [GroupId] UNIQUEIDENTIFIER NULL,
        [SessionStart] DATETIME2 NULL,
   [SessionEnd] DATETIME2 NULL
    );
    PRINT 'Table [pulse].[Sessions] created';
END
GO

-- Activities Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[Activities]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[Activities] (
      [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
        [Order] INT NOT NULL,
        [Type] INT NOT NULL,
     [Title] NVARCHAR(200) NOT NULL,
        [Prompt] NVARCHAR(1000) NULL,
        [ConfigJson] NVARCHAR(MAX) NULL,
      [Status] INT NOT NULL DEFAULT 0,
        [OpenedAt] DATETIMEOFFSET NULL,
        [ClosedAt] DATETIMEOFFSET NULL,
        [DurationMinutes] INT NULL
    );
    PRINT 'Table [pulse].[Activities] created';
END
GO

-- Participants Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[Participants]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[Participants] (
  [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
        [DisplayName] NVARCHAR(120) NULL,
        [IsAnonymous] BIT NOT NULL DEFAULT 0,
        [DimensionsJson] NVARCHAR(MAX) NOT NULL,
        [Token] NVARCHAR(64) NULL,
        [JoinedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[Participants] created';
END
GO

-- Responses Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[Responses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[Responses] (
      [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
        [ActivityId] UNIQUEIDENTIFIER NOT NULL,
        [ParticipantId] UNIQUEIDENTIFIER NOT NULL,
     [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [DimensionsJson] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[Responses] created';
END
GO

-- ContributionCounters Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[ContributionCounters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[ContributionCounters] (
        [ParticipantId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
  [TotalContributions] INT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[ContributionCounters] created';
END
GO

PRINT '';
GO

-- =============================================
-- Step 3: Create Indexes
-- =============================================
PRINT 'Step 3: Creating indexes...';
GO

-- FacilitatorUsers Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUsers_Email' AND object_id = OBJECT_ID('[pulse].[FacilitatorUsers]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_FacilitatorUsers_Email] ON [pulse].[FacilitatorUsers] ([Email]);
    PRINT 'Index [IX_FacilitatorUsers_Email] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUsers_CreatedAt' AND object_id = OBJECT_ID('[pulse].[FacilitatorUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FacilitatorUsers_CreatedAt] ON [pulse].[FacilitatorUsers] ([CreatedAt]);
END
GO

-- FacilitatorUserData Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUserData_FacilitatorUserId_Key' AND object_id = OBJECT_ID('[pulse].[FacilitatorUserData]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_FacilitatorUserData_FacilitatorUserId_Key] ON [pulse].[FacilitatorUserData] ([FacilitatorUserId], [Key]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUserData_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[FacilitatorUserData]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FacilitatorUserData_FacilitatorUserId] ON [pulse].[FacilitatorUserData] ([FacilitatorUserId]);
END
GO

-- LoginOtps Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_Email_OtpCode' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_Email_OtpCode] ON [pulse].[LoginOtps] ([Email], [OtpCode]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_Email_CreatedAt' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_Email_CreatedAt] ON [pulse].[LoginOtps] ([Email], [CreatedAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_ExpiresAt' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_ExpiresAt] ON [pulse].[LoginOtps] ([ExpiresAt]);
END
GO

-- SessionGroups Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_FacilitatorUserId] ON [pulse].[SessionGroups] ([FacilitatorUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_ParentGroupId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_ParentGroupId] ON [pulse].[SessionGroups] ([ParentGroupId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId] ON [pulse].[SessionGroups] ([FacilitatorUserId], [Level], [ParentGroupId]);
END
GO

-- SessionTemplates Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_Category' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_Category] ON [pulse].[SessionTemplates] ([Category]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_IsSystemTemplate' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_IsSystemTemplate] ON [pulse].[SessionTemplates] ([IsSystemTemplate]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_CreatedByUserId' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_CreatedByUserId] ON [pulse].[SessionTemplates] ([CreatedByUserId]);
END
GO

-- Sessions Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_Code' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
  CREATE UNIQUE NONCLUSTERED INDEX [IX_Sessions_Code] ON [pulse].[Sessions] ([Code]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_Status' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_Status] ON [pulse].[Sessions] ([Status]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_ExpiresAt' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_ExpiresAt] ON [pulse].[Sessions] ([ExpiresAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_FacilitatorUserId] ON [pulse].[Sessions] ([FacilitatorUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_GroupId' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
  CREATE NONCLUSTERED INDEX [IX_Sessions_GroupId] ON [pulse].[Sessions] ([GroupId]);
END
GO

-- Activities Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_SessionId_Order' AND object_id = OBJECT_ID('[pulse].[Activities]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Activities_SessionId_Order] ON [pulse].[Activities] ([SessionId], [Order]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_SessionId_Status' AND object_id = OBJECT_ID('[pulse].[Activities]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Activities_SessionId_Status] ON [pulse].[Activities] ([SessionId], [Status]);
END
GO

-- Participants Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Participants_SessionId_JoinedAt' AND object_id = OBJECT_ID('[pulse].[Participants]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Participants_SessionId_JoinedAt] ON [pulse].[Participants] ([SessionId], [JoinedAt]);
END
GO

-- Responses Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Responses_SessionId_ActivityId_CreatedAt' AND object_id = OBJECT_ID('[pulse].[Responses]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Responses_SessionId_ActivityId_CreatedAt] ON [pulse].[Responses] ([SessionId], [ActivityId], [CreatedAt]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Responses_ParticipantId_CreatedAt' AND object_id = OBJECT_ID('[pulse].[Responses]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Responses_ParticipantId_CreatedAt] ON [pulse].[Responses] ([ParticipantId], [CreatedAt]);
END
GO

-- ContributionCounters Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ContributionCounters_SessionId' AND object_id = OBJECT_ID('[pulse].[ContributionCounters]'))
BEGIN
  CREATE NONCLUSTERED INDEX [IX_ContributionCounters_SessionId] ON [pulse].[ContributionCounters] ([SessionId]);
END
GO

PRINT '';
GO

-- =============================================
-- Step 4: Verification
-- =============================================
PRINT 'Step 4: Verifying setup...';
GO

DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*) 
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'pulse';

PRINT 'Tables in [pulse] schema: ' + CAST(@TableCount AS VARCHAR);

DECLARE @IndexCount INT;
SELECT @IndexCount = COUNT(*) 
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'pulse' AND i.type > 0; -- Exclude heap indexes

PRINT 'Indexes in [pulse] schema: ' + CAST(@IndexCount AS VARCHAR);
PRINT '';
GO

-- =============================================
-- Summary
-- =============================================
PRINT '========================================';
PRINT 'TechWayFit Pulse Database Setup Complete';
PRINT '========================================';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
PRINT 'Tables created:';
PRINT '  - pulse.FacilitatorUsers';
PRINT '  - pulse.FacilitatorUserData';
PRINT '  - pulse.LoginOtps';
PRINT '  - pulse.SessionGroups';
PRINT '  - pulse.SessionTemplates';
PRINT '  - pulse.Sessions';
PRINT '  - pulse.Activities';
PRINT '  - pulse.Participants';
PRINT '  - pulse.Responses';
PRINT '  - pulse.ContributionCounters';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Update connection string in appsettings.json';
PRINT '  2. Set Pulse:DatabaseProvider to "SqlServer"';
PRINT '  3. Restart application';
PRINT '========================================';
GO
