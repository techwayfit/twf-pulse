-- =============================================
-- TechWayFit Pulse - Create Indexes Script
-- Version 1.0
-- =============================================
-- Description: Creates all indexes for optimal query performance
-- =============================================

PRINT 'Creating indexes for TechWayFit Pulse database...';
GO

-- =============================================
-- Indexes for FacilitatorUsers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUsers_Email' AND object_id = OBJECT_ID('[pulse].[FacilitatorUsers]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_FacilitatorUsers_Email]
     ON [pulse].[FacilitatorUsers] ([Email]);
    PRINT 'Index [IX_FacilitatorUsers_Email] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUsers_CreatedAt' AND object_id = OBJECT_ID('[pulse].[FacilitatorUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FacilitatorUsers_CreatedAt]
 ON [pulse].[FacilitatorUsers] ([CreatedAt]);
    PRINT 'Index [IX_FacilitatorUsers_CreatedAt] created';
END
GO

-- =============================================
-- Indexes for FacilitatorUserData
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUserData_FacilitatorUserId_Key' AND object_id = OBJECT_ID('[pulse].[FacilitatorUserData]'))
BEGIN
  CREATE UNIQUE NONCLUSTERED INDEX [IX_FacilitatorUserData_FacilitatorUserId_Key]
        ON [pulse].[FacilitatorUserData] ([FacilitatorUserId], [Key]);
    PRINT 'Index [IX_FacilitatorUserData_FacilitatorUserId_Key] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FacilitatorUserData_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[FacilitatorUserData]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FacilitatorUserData_FacilitatorUserId]
        ON [pulse].[FacilitatorUserData] ([FacilitatorUserId]);
    PRINT 'Index [IX_FacilitatorUserData_FacilitatorUserId] created';
END
GO

-- =============================================
-- Indexes for LoginOtps
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_Email_OtpCode' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_Email_OtpCode]
        ON [pulse].[LoginOtps] ([Email], [OtpCode]);
    PRINT 'Index [IX_LoginOtps_Email_OtpCode] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_Email_CreatedAt' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_Email_CreatedAt]
        ON [pulse].[LoginOtps] ([Email], [CreatedAt]);
    PRINT 'Index [IX_LoginOtps_Email_CreatedAt] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginOtps_ExpiresAt' AND object_id = OBJECT_ID('[pulse].[LoginOtps]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LoginOtps_ExpiresAt]
        ON [pulse].[LoginOtps] ([ExpiresAt]);
    PRINT 'Index [IX_LoginOtps_ExpiresAt] created';
END
GO

-- =============================================
-- Indexes for SessionGroups
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_FacilitatorUserId]
    ON [pulse].[SessionGroups] ([FacilitatorUserId]);
    PRINT 'Index [IX_SessionGroups_FacilitatorUserId] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_ParentGroupId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_ParentGroupId]
        ON [pulse].[SessionGroups] ([ParentGroupId]);
    PRINT 'Index [IX_SessionGroups_ParentGroupId] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId' AND object_id = OBJECT_ID('[pulse].[SessionGroups]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId]
        ON [pulse].[SessionGroups] ([FacilitatorUserId], [Level], [ParentGroupId]);
    PRINT 'Index [IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId] created';
END
GO

-- =============================================
-- Indexes for SessionTemplates
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_Category' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_Category]
        ON [pulse].[SessionTemplates] ([Category]);
    PRINT 'Index [IX_SessionTemplates_Category] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_IsSystemTemplate' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_IsSystemTemplate]
      ON [pulse].[SessionTemplates] ([IsSystemTemplate]);
  PRINT 'Index [IX_SessionTemplates_IsSystemTemplate] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SessionTemplates_CreatedByUserId' AND object_id = OBJECT_ID('[pulse].[SessionTemplates]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SessionTemplates_CreatedByUserId]
        ON [pulse].[SessionTemplates] ([CreatedByUserId]);
    PRINT 'Index [IX_SessionTemplates_CreatedByUserId] created';
END
GO

-- =============================================
-- Indexes for Sessions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_Code' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Sessions_Code]
     ON [pulse].[Sessions] ([Code]);
    PRINT 'Index [IX_Sessions_Code] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_Status' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
  CREATE NONCLUSTERED INDEX [IX_Sessions_Status]
        ON [pulse].[Sessions] ([Status]);
    PRINT 'Index [IX_Sessions_Status] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_ExpiresAt' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_ExpiresAt]
        ON [pulse].[Sessions] ([ExpiresAt]);
    PRINT 'Index [IX_Sessions_ExpiresAt] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_FacilitatorUserId' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_FacilitatorUserId]
        ON [pulse].[Sessions] ([FacilitatorUserId]);
    PRINT 'Index [IX_Sessions_FacilitatorUserId] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sessions_GroupId' AND object_id = OBJECT_ID('[pulse].[Sessions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Sessions_GroupId]
        ON [pulse].[Sessions] ([GroupId]);
    PRINT 'Index [IX_Sessions_GroupId] created';
END
GO

-- =============================================
-- Indexes for Activities
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_SessionId_Order' AND object_id = OBJECT_ID('[pulse].[Activities]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Activities_SessionId_Order]
   ON [pulse].[Activities] ([SessionId], [Order]);
    PRINT 'Index [IX_Activities_SessionId_Order] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_SessionId_Status' AND object_id = OBJECT_ID('[pulse].[Activities]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Activities_SessionId_Status]
        ON [pulse].[Activities] ([SessionId], [Status]);
    PRINT 'Index [IX_Activities_SessionId_Status] created';
END
GO

-- =============================================
-- Indexes for Participants
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Participants_SessionId_JoinedAt' AND object_id = OBJECT_ID('[pulse].[Participants]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Participants_SessionId_JoinedAt]
      ON [pulse].[Participants] ([SessionId], [JoinedAt]);
    PRINT 'Index [IX_Participants_SessionId_JoinedAt] created';
END
GO

-- =============================================
-- Indexes for Responses
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Responses_SessionId_ActivityId_CreatedAt' AND object_id = OBJECT_ID('[pulse].[Responses]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Responses_SessionId_ActivityId_CreatedAt]
        ON [pulse].[Responses] ([SessionId], [ActivityId], [CreatedAt]);
    PRINT 'Index [IX_Responses_SessionId_ActivityId_CreatedAt] created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Responses_ParticipantId_CreatedAt' AND object_id = OBJECT_ID('[pulse].[Responses]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Responses_ParticipantId_CreatedAt]
        ON [pulse].[Responses] ([ParticipantId], [CreatedAt]);
    PRINT 'Index [IX_Responses_ParticipantId_CreatedAt] created';
END
GO

-- =============================================
-- Indexes for ContributionCounters
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ContributionCounters_SessionId' AND object_id = OBJECT_ID('[pulse].[ContributionCounters]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContributionCounters_SessionId]
        ON [pulse].[ContributionCounters] ([SessionId]);
    PRINT 'Index [IX_ContributionCounters_SessionId] created';
END
GO

PRINT 'All indexes created successfully';
GO
