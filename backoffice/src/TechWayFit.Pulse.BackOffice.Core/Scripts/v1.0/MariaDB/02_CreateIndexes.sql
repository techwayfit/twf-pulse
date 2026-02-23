-- =============================================
-- TechWayFit Pulse BackOffice - Create Indexes (MariaDB/MySQL)
-- Version 1.0
-- =============================================
-- Description: Creates indexes for BackOffice tables matching EF Core configuration.
--              Idempotent: uses DROP INDEX IF EXISTS before each CREATE INDEX.
-- =============================================

USE `pulse`;

SELECT 'Creating indexes for BackOffice tables...';

-- =============================================
-- Indexes for BackOfficeUsers
-- =============================================

-- Unique index on Username (login lookup + uniqueness enforcement)
DROP INDEX IF EXISTS `UIX_BackOfficeUsers_Username` ON `BackOfficeUsers`;
CREATE UNIQUE INDEX `UIX_BackOfficeUsers_Username` ON `BackOfficeUsers` (`Username`);
SELECT 'Index `UIX_BackOfficeUsers_Username` created';

-- Role + IsActive filter for operator listing
DROP INDEX IF EXISTS `IX_BackOfficeUsers_Role_IsActive` ON `BackOfficeUsers`;
CREATE INDEX `IX_BackOfficeUsers_Role_IsActive` ON `BackOfficeUsers` (`Role`, `IsActive`);
SELECT 'Index `IX_BackOfficeUsers_Role_IsActive` created';

-- =============================================
-- Indexes for AuditLogs
-- =============================================

-- EntityType + EntityId — most common audit search: "show all actions on this entity"
DROP INDEX IF EXISTS `IX_AuditLogs_EntityType_EntityId` ON `AuditLogs`;
CREATE INDEX `IX_AuditLogs_EntityType_EntityId` ON `AuditLogs` (`EntityType`, `EntityId`);
SELECT 'Index `IX_AuditLogs_EntityType_EntityId` created';

-- OperatorId — "show all actions by this operator"
DROP INDEX IF EXISTS `IX_AuditLogs_OperatorId` ON `AuditLogs`;
CREATE INDEX `IX_AuditLogs_OperatorId` ON `AuditLogs` (`OperatorId`);
SELECT 'Index `IX_AuditLogs_OperatorId` created';

-- OccurredAt — date-range queries and chronological display
DROP INDEX IF EXISTS `IX_AuditLogs_OccurredAt` ON `AuditLogs`;
CREATE INDEX `IX_AuditLogs_OccurredAt` ON `AuditLogs` (`OccurredAt` DESC);
SELECT 'Index `IX_AuditLogs_OccurredAt` created';

-- Action + OccurredAt — "show all ForceEndSession actions" sorted by date
DROP INDEX IF EXISTS `IX_AuditLogs_Action_OccurredAt` ON `AuditLogs`;
CREATE INDEX `IX_AuditLogs_Action_OccurredAt` ON `AuditLogs` (`Action`, `OccurredAt` DESC);
SELECT 'Index `IX_AuditLogs_Action_OccurredAt` created';

SELECT '';
SELECT 'BackOffice indexes created/verified successfully.';
