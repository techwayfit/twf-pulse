-- =============================================
-- TechWayFit Pulse BackOffice - Create Indexes
-- Version 1.0
-- =============================================
-- Description: Creates indexes for BackOffice tables matching EF Core configuration.
--              Safe to re-run (idempotent IF NOT EXISTS guards).
-- =============================================

PRINT 'Creating indexes for BackOffice tables...';
GO

-- =============================================
-- Indexes for pulse.BackOfficeUsers
-- =============================================

-- Unique index on Username (login lookup + uniqueness enforcement)
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UIX_BackOfficeUsers_Username'
      AND object_id = OBJECT_ID('[pulse].[BackOfficeUsers]')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UIX_BackOfficeUsers_Username]
        ON [pulse].[BackOfficeUsers] ([Username]);
    PRINT 'Index [UIX_BackOfficeUsers_Username] created';
END
ELSE
    PRINT 'Index [UIX_BackOfficeUsers_Username] already exists';
GO

-- Role filter for operator listing
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_BackOfficeUsers_Role_IsActive'
      AND object_id = OBJECT_ID('[pulse].[BackOfficeUsers]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BackOfficeUsers_Role_IsActive]
        ON [pulse].[BackOfficeUsers] ([Role], [IsActive]);
    PRINT 'Index [IX_BackOfficeUsers_Role_IsActive] created';
END
ELSE
    PRINT 'Index [IX_BackOfficeUsers_Role_IsActive] already exists';
GO

-- =============================================
-- Indexes for pulse.AuditLogs
-- =============================================

-- EntityType + EntityId — most common audit search: "show all actions on this entity"
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AuditLogs_EntityType_EntityId'
      AND object_id = OBJECT_ID('[pulse].[AuditLogs]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityType_EntityId]
        ON [pulse].[AuditLogs] ([EntityType], [EntityId])
        INCLUDE ([Action], [OperatorId], [OccurredAt]);
    PRINT 'Index [IX_AuditLogs_EntityType_EntityId] created';
END
ELSE
    PRINT 'Index [IX_AuditLogs_EntityType_EntityId] already exists';
GO

-- OperatorId — "show all actions by this operator"
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AuditLogs_OperatorId'
      AND object_id = OBJECT_ID('[pulse].[AuditLogs]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_OperatorId]
        ON [pulse].[AuditLogs] ([OperatorId])
        INCLUDE ([Action], [EntityType], [EntityId], [OccurredAt]);
    PRINT 'Index [IX_AuditLogs_OperatorId] created';
END
ELSE
    PRINT 'Index [IX_AuditLogs_OperatorId] already exists';
GO

-- OccurredAt — date-range queries and chronological display
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AuditLogs_OccurredAt'
      AND object_id = OBJECT_ID('[pulse].[AuditLogs]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_OccurredAt]
        ON [pulse].[AuditLogs] ([OccurredAt] DESC);
    PRINT 'Index [IX_AuditLogs_OccurredAt] created';
END
ELSE
    PRINT 'Index [IX_AuditLogs_OccurredAt] already exists';
GO

-- Action filter — "show all ForceEndSession actions"
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AuditLogs_Action_OccurredAt'
      AND object_id = OBJECT_ID('[pulse].[AuditLogs]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action_OccurredAt]
        ON [pulse].[AuditLogs] ([Action], [OccurredAt] DESC);
    PRINT 'Index [IX_AuditLogs_Action_OccurredAt] created';
END
ELSE
    PRINT 'Index [IX_AuditLogs_Action_OccurredAt] already exists';
GO

PRINT '';
PRINT 'BackOffice indexes created successfully.';
GO
