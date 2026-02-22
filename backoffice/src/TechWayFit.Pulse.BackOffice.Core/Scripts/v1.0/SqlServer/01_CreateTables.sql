-- =============================================
-- TechWayFit Pulse BackOffice - Create Tables
-- Version 1.0
-- =============================================
-- Description: Creates BackOffice-exclusive tables in the [pulse] schema.
--              Safe to re-run (idempotent IF NOT EXISTS guards).
-- =============================================

-- =============================================
-- Table: pulse.BackOfficeUsers
-- Description: Operator accounts for the BackOffice portal.
--              Separate from FacilitatorUsers — these are internal staff.
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[pulse].[BackOfficeUsers]') AND type = N'U'
)
BEGIN
    CREATE TABLE [pulse].[BackOfficeUsers] (
        [Id]           UNIQUEIDENTIFIER  NOT NULL  CONSTRAINT [PK_BackOfficeUsers] PRIMARY KEY DEFAULT NEWID(),
        [Username]     NVARCHAR(128)     NOT NULL,
        [PasswordHash] NVARCHAR(256)     NOT NULL,
        [Role]         NVARCHAR(50)      NOT NULL,   -- 'Operator' | 'SuperAdmin'
        [IsActive]     BIT               NOT NULL  DEFAULT 1,
        [CreatedAt]    DATETIMEOFFSET    NOT NULL  DEFAULT SYSDATETIMEOFFSET(),
        [LastLoginAt]  DATETIMEOFFSET    NULL
    );
    PRINT 'Table [pulse].[BackOfficeUsers] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[BackOfficeUsers] already exists';
END
GO

-- =============================================
-- Table: pulse.AuditLogs
-- Description: Immutable audit trail for every BackOffice operator action.
--              Rows must never be deleted; archive-only after retention period.
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[pulse].[AuditLogs]') AND type = N'U'
)
BEGIN
    CREATE TABLE [pulse].[AuditLogs] (
        [Id]           UNIQUEIDENTIFIER  NOT NULL  CONSTRAINT [PK_AuditLogs] PRIMARY KEY DEFAULT NEWID(),
        [OperatorId]   NVARCHAR(256)     NOT NULL,   -- BackOfficeUser.Id or username
        [OperatorRole] NVARCHAR(50)      NOT NULL,   -- 'Operator' | 'SuperAdmin'
        [Action]       NVARCHAR(128)     NOT NULL,   -- e.g. 'DisableUser', 'ForceEndSession'
        [EntityType]   NVARCHAR(128)     NOT NULL,   -- e.g. 'FacilitatorUser', 'Session'
        [EntityId]     NVARCHAR(256)     NOT NULL,   -- GUID or other identifier
        [FieldName]    NVARCHAR(128)     NULL,        -- for field-level changes
        [OldValue]     NVARCHAR(MAX)     NULL,
        [NewValue]     NVARCHAR(MAX)     NULL,
        [Reason]       NVARCHAR(MAX)     NULL,        -- operator-supplied reason
        [IpAddress]    NVARCHAR(64)      NOT NULL,
        [OccurredAt]   DATETIMEOFFSET    NOT NULL  DEFAULT SYSDATETIMEOFFSET()
    );
    PRINT 'Table [pulse].[AuditLogs] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [pulse].[AuditLogs] already exists';
END
GO
