-- =============================================
-- TechWayFit Pulse - Create Tables Script
-- Version 1.0
-- =============================================

-- =============================================
-- Table: pulse.FacilitatorUsers
-- Description: Stores facilitator user accounts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[FacilitatorUsers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[FacilitatorUsers] (
      [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Email] NVARCHAR(256) NOT NULL,
        [DisplayName] NVARCHAR(120) NOT NULL,
      [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [LastLoginAt] DATETIMEOFFSET NULL
    );
    PRINT 'Table [pulse].[FacilitatorUsers] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[FacilitatorUsers] already exists';
END
GO

-- =============================================
-- Table: pulse.FacilitatorUserData
-- Description: Key-value storage for facilitator preferences
-- =============================================
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
    PRINT 'Table [pulse].[FacilitatorUserData] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[FacilitatorUserData] already exists';
END
GO

-- =============================================
-- Table: pulse.LoginOtps
-- Description: Stores one-time passwords for authentication
-- =============================================
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
    PRINT 'Table [pulse].[LoginOtps] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[LoginOtps] already exists';
END
GO

-- =============================================
-- Table: pulse.SessionGroups
-- Description: Hierarchical organization of sessions
-- =============================================
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
    PRINT 'Table [pulse].[SessionGroups] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[SessionGroups] already exists';
END
GO

-- =============================================
-- Table: pulse.SessionTemplates
-- Description: Reusable session templates
-- =============================================
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
    PRINT 'Table [pulse].[SessionTemplates] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[SessionTemplates] already exists';
END
GO

-- =============================================
-- Table: pulse.Sessions
-- Description: Workshop sessions
-- =============================================
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
    PRINT 'Table [pulse].[Sessions] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[Sessions] already exists';
END
GO

-- =============================================
-- Table: pulse.Activities
-- Description: Workshop activities within sessions
-- =============================================
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
    PRINT 'Table [pulse].[Activities] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[Activities] already exists';
END
GO

-- =============================================
-- Table: pulse.Participants
-- Description: Workshop participants
-- =============================================
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
    PRINT 'Table [pulse].[Participants] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[Participants] already exists';
END
GO

-- =============================================
-- Table: pulse.Responses
-- Description: Participant responses to activities
-- =============================================
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
    PRINT 'Table [pulse].[Responses] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[Responses] already exists';
END
GO

-- =============================================
-- Table: pulse.ContributionCounters
-- Description: Tracks participant contribution counts
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[ContributionCounters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [pulse].[ContributionCounters] (
 [ParticipantId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
        [TotalContributions] INT NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[ContributionCounters] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[ContributionCounters] already exists';
END
GO
