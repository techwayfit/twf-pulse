-- =============================================
-- TechWayFit Pulse BackOffice - Create Tables (MariaDB/MySQL)
-- Version 1.0
-- =============================================
-- Description: Creates BackOffice-exclusive tables in the `pulse` database.
--              Safe to re-run (idempotent CREATE TABLE IF NOT EXISTS guards).
-- Prerequisite: Run the main application's MariaDB V1.0 setup first so
--               the `pulse` database and all main-app tables already exist.
-- =============================================

USE `pulse`;

-- =============================================
-- Table: BackOfficeUsers
-- Description: Operator accounts for the BackOffice portal.
--              Separate from FacilitatorUsers — these are internal staff.
-- =============================================
CREATE TABLE IF NOT EXISTS `BackOfficeUsers` (
    `Id`           CHAR(36)       NOT NULL PRIMARY KEY,
    `Username`     VARCHAR(128)   NOT NULL,
    `PasswordHash` VARCHAR(256)   NOT NULL,
    `Role`         VARCHAR(50)    NOT NULL,   -- 'Operator' | 'SuperAdmin'
    `IsActive`     TINYINT(1)     NOT NULL  DEFAULT 1,
    `CreatedAt`    DATETIME(6)    NOT NULL  DEFAULT CURRENT_TIMESTAMP(6),
    `LastLoginAt`  DATETIME(6)    NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`BackOfficeUsers` created/verified';

-- =============================================
-- Table: AuditLogs
-- Description: Immutable audit trail for every BackOffice operator action.
--              Rows must never be deleted; archive-only after retention period.
-- =============================================
CREATE TABLE IF NOT EXISTS `AuditLogs` (
    `Id`           CHAR(36)       NOT NULL PRIMARY KEY,
    `OperatorId`   VARCHAR(256)   NOT NULL,   -- BackOfficeUser.Id or username
    `OperatorRole` VARCHAR(50)    NOT NULL,   -- 'Operator' | 'SuperAdmin'
    `Action`       VARCHAR(128)   NOT NULL,   -- e.g. 'DisableUser', 'ForceEndSession'
    `EntityType`   VARCHAR(128)   NOT NULL,   -- e.g. 'FacilitatorUser', 'Session'
    `EntityId`     VARCHAR(256)   NOT NULL,   -- GUID or other identifier
    `FieldName`    VARCHAR(128)   NULL,        -- for field-level changes
    `OldValue`     LONGTEXT       NULL,
    `NewValue`     LONGTEXT       NULL,
    `Reason`       LONGTEXT       NULL,        -- operator-supplied reason
    `IpAddress`    VARCHAR(64)    NOT NULL,
    `OccurredAt`   DATETIME(6)    NOT NULL  DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`AuditLogs` created/verified';
