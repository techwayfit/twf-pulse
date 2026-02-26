-- =============================================
-- Migration: Add SessionActivityMetadata table
-- Version: 1.0 Patch 01
-- Date: 2025
-- Description: Stores generic key-value transient runtime metadata
--              per (session, activity) pair. Supports any activity type.
--              Intentionally NOT a foreign-keyed child of Sessions/Activities
--              so that it survives session copy operations.
-- =============================================

USE `pulse`;

-- =============================================
-- SessionActivityMetadata Table
-- =============================================
CREATE TABLE IF NOT EXISTS `SessionActivityMetadata` (
    `Id`         CHAR(36)     NOT NULL,
    `SessionId`  CHAR(36)     NOT NULL,
    `ActivityId` CHAR(36)     NOT NULL,
    `Key`        VARCHAR(100) NOT NULL,
    `Value`      TEXT         NOT NULL,
    `CreatedAt`  DATETIME(6)  NOT NULL,
    `UpdatedAt`  DATETIME(6)  NOT NULL,
    CONSTRAINT `PK_SessionActivityMetadata` PRIMARY KEY (`Id`),
    CONSTRAINT `UQ_SessionActivityMetadata_Session_Activity_Key`
        UNIQUE (`SessionId`, `ActivityId`, `Key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- Indexes
-- =============================================
DROP INDEX IF EXISTS `IX_SessionActivityMetadata_SessionId_ActivityId` ON `SessionActivityMetadata`;
CREATE INDEX `IX_SessionActivityMetadata_SessionId_ActivityId`
    ON `SessionActivityMetadata` (`SessionId`, `ActivityId`);

SELECT 'SessionActivityMetadata table and indexes created/verified successfully';
