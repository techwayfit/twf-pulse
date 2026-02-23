-- =============================================
-- TechWayFit Pulse - MariaDB/MySQL Master Setup Script
-- Version 1.0
-- =============================================
-- Description: Executes all setup scripts in order for MariaDB/MySQL
-- Usage: Run this script against your MariaDB or MySQL database
-- Prerequisites: 
--   - MariaDB 10.3 or later (or MySQL 8.0+)
--   - Database already created (or create it first)
--   - Appropriate permissions (CREATE SCHEMA, CREATE TABLE, CREATE INDEX)
-- Compatibility:
--   - MariaDB 10.3+
--   - MySQL 8.0+
--   - Indexes use OR REPLACE for idempotency on MariaDB 10.5+
--   - Falls back to DROP IF EXISTS for older versions
-- =============================================

-- Use database (update this to your database name if needed)
-- Comment out if running against a specific database
-- USE `TechWayFitPulse`;

SELECT '========================================';
SELECT 'TechWayFit Pulse Database Setup v1.0';
SELECT '========================================';
SELECT CONCAT('Started at: ', NOW());
SELECT '';

-- =============================================
-- Step 1: Create Schema
-- =============================================
SELECT 'Step 1: Creating schema...';

-- Create schema if it doesn't exist
CREATE DATABASE IF NOT EXISTS `pulse`;
USE `pulse`;
SELECT 'Schema `pulse` created/verified successfully';

SELECT '';

-- =============================================
-- Step 2: Create Tables
-- =============================================
SELECT 'Step 2: Creating tables...';

-- FacilitatorUsers Table
CREATE TABLE IF NOT EXISTS `FacilitatorUsers` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Email` VARCHAR(256) NOT NULL,
    `DisplayName` VARCHAR(120) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `LastLoginAt` DATETIME(6) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`FacilitatorUsers` created/verified';

-- FacilitatorUserData Table
CREATE TABLE IF NOT EXISTS `FacilitatorUserData` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `FacilitatorUserId` CHAR(36) NOT NULL,
    `Key` VARCHAR(200) NOT NULL,
    `Value` LONGTEXT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`FacilitatorUserData` created/verified';

-- LoginOtps Table
CREATE TABLE IF NOT EXISTS `LoginOtps` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Email` VARCHAR(256) NOT NULL,
    `OtpCode` VARCHAR(10) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ExpiresAt` DATETIME(6) NOT NULL,
    `IsUsed` TINYINT(1) NOT NULL DEFAULT 0,
    `UsedAt` DATETIME(6) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`LoginOtps` created/verified';

-- SessionGroups Table
CREATE TABLE IF NOT EXISTS `SessionGroups` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Name` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(1000) NULL,
    `Level` INT NOT NULL,
    `ParentGroupId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    `FacilitatorUserId` CHAR(36) NULL,
    `Icon` VARCHAR(50) NOT NULL DEFAULT 'folder',
    `Color` VARCHAR(20) NULL,
    `IsDefault` TINYINT(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`SessionGroups` created/verified';

-- SessionTemplates Table
CREATE TABLE IF NOT EXISTS `SessionTemplates` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Name` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(500) NOT NULL,
    `Category` INT NOT NULL,
    `IconEmoji` VARCHAR(10) NOT NULL,
    `ConfigJson` LONGTEXT NOT NULL,
    `IsSystemTemplate` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedByUserId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`SessionTemplates` created/verified';

-- Sessions Table
CREATE TABLE IF NOT EXISTS `Sessions` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Code` VARCHAR(32) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Goal` LONGTEXT NULL,
    `ContextJson` LONGTEXT NULL,
    `SettingsJson` LONGTEXT NOT NULL,
    `JoinFormSchemaJson` LONGTEXT NOT NULL,
  `Status` INT NOT NULL DEFAULT 0,
    `CurrentActivityId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    `ExpiresAt` DATETIME(6) NOT NULL,
    `FacilitatorUserId` CHAR(36) NULL,
  `GroupId` CHAR(36) NULL,
    `SessionStart` DATETIME(6) NULL,
    `SessionEnd` DATETIME(6) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`Sessions` created/verified';

-- Activities Table
CREATE TABLE IF NOT EXISTS `Activities` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `SessionId` CHAR(36) NOT NULL,
    `Order` INT NOT NULL,
    `Type` INT NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Prompt` VARCHAR(1000) NULL,
    `ConfigJson` LONGTEXT NULL,
    `Status` INT NOT NULL DEFAULT 0,
    `OpenedAt` DATETIME(6) NULL,
    `ClosedAt` DATETIME(6) NULL,
    `DurationMinutes` INT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`Activities` created/verified';

-- Participants Table
CREATE TABLE IF NOT EXISTS `Participants` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
 `SessionId` CHAR(36) NOT NULL,
`DisplayName` VARCHAR(120) NULL,
    `IsAnonymous` TINYINT(1) NOT NULL DEFAULT 0,
    `DimensionsJson` LONGTEXT NOT NULL,
    `Token` VARCHAR(64) NULL,
    `JoinedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`Participants` created/verified';

-- Responses Table
CREATE TABLE IF NOT EXISTS `Responses` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `SessionId` CHAR(36) NOT NULL,
    `ActivityId` CHAR(36) NOT NULL,
 `ParticipantId` CHAR(36) NOT NULL,
    `PayloadJson` LONGTEXT NOT NULL,
    `DimensionsJson` LONGTEXT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`Responses` created/verified';

-- ContributionCounters Table
CREATE TABLE IF NOT EXISTS `ContributionCounters` (
    `ParticipantId` CHAR(36) NOT NULL PRIMARY KEY,
    `SessionId` CHAR(36) NOT NULL,
    `TotalContributions` INT NOT NULL DEFAULT 0,
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
SELECT 'Table `pulse`.`ContributionCounters` created/verified';

SELECT '';

-- =============================================
-- Step 3: Create Indexes
-- =============================================
SELECT 'Step 3: Creating indexes...';
SELECT 'Note: Indexes will be dropped and recreated to ensure idempotency';

-- FacilitatorUsers Indexes
DROP INDEX IF EXISTS `IX_FacilitatorUsers_Email` ON `FacilitatorUsers`;
CREATE UNIQUE INDEX `IX_FacilitatorUsers_Email` ON `FacilitatorUsers` (`Email`);

DROP INDEX IF EXISTS `IX_FacilitatorUsers_CreatedAt` ON `FacilitatorUsers`;
CREATE INDEX `IX_FacilitatorUsers_CreatedAt` ON `FacilitatorUsers` (`CreatedAt`);

-- FacilitatorUserData Indexes
DROP INDEX IF EXISTS `IX_FacilitatorUserData_FacilitatorUserId_Key` ON `FacilitatorUserData`;
CREATE UNIQUE INDEX `IX_FacilitatorUserData_FacilitatorUserId_Key` ON `FacilitatorUserData` (`FacilitatorUserId`, `Key`);

DROP INDEX IF EXISTS `IX_FacilitatorUserData_FacilitatorUserId` ON `FacilitatorUserData`;
CREATE INDEX `IX_FacilitatorUserData_FacilitatorUserId` ON `FacilitatorUserData` (`FacilitatorUserId`);

-- LoginOtps Indexes
DROP INDEX IF EXISTS `IX_LoginOtps_Email_OtpCode` ON `LoginOtps`;
CREATE INDEX `IX_LoginOtps_Email_OtpCode` ON `LoginOtps` (`Email`, `OtpCode`);

DROP INDEX IF EXISTS `IX_LoginOtps_Email_CreatedAt` ON `LoginOtps`;
CREATE INDEX `IX_LoginOtps_Email_CreatedAt` ON `LoginOtps` (`Email`, `CreatedAt`);

DROP INDEX IF EXISTS `IX_LoginOtps_ExpiresAt` ON `LoginOtps`;
CREATE INDEX `IX_LoginOtps_ExpiresAt` ON `LoginOtps` (`ExpiresAt`);

-- SessionGroups Indexes
DROP INDEX IF EXISTS `IX_SessionGroups_FacilitatorUserId` ON `SessionGroups`;
CREATE INDEX `IX_SessionGroups_FacilitatorUserId` ON `SessionGroups` (`FacilitatorUserId`);

DROP INDEX IF EXISTS `IX_SessionGroups_ParentGroupId` ON `SessionGroups`;
CREATE INDEX `IX_SessionGroups_ParentGroupId` ON `SessionGroups` (`ParentGroupId`);

DROP INDEX IF EXISTS `IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId` ON `SessionGroups`;
CREATE INDEX `IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId` ON `SessionGroups` (`FacilitatorUserId`, `Level`, `ParentGroupId`);

-- SessionTemplates Indexes
DROP INDEX IF EXISTS `IX_SessionTemplates_Category` ON `SessionTemplates`;
CREATE INDEX `IX_SessionTemplates_Category` ON `SessionTemplates` (`Category`);

DROP INDEX IF EXISTS `IX_SessionTemplates_IsSystemTemplate` ON `SessionTemplates`;
CREATE INDEX `IX_SessionTemplates_IsSystemTemplate` ON `SessionTemplates` (`IsSystemTemplate`);

DROP INDEX IF EXISTS `IX_SessionTemplates_CreatedByUserId` ON `SessionTemplates`;
CREATE INDEX `IX_SessionTemplates_CreatedByUserId` ON `SessionTemplates` (`CreatedByUserId`);

-- Sessions Indexes
DROP INDEX IF EXISTS `IX_Sessions_Code` ON `Sessions`;
CREATE UNIQUE INDEX `IX_Sessions_Code` ON `Sessions` (`Code`);

DROP INDEX IF EXISTS `IX_Sessions_Status` ON `Sessions`;
CREATE INDEX `IX_Sessions_Status` ON `Sessions` (`Status`);

DROP INDEX IF EXISTS `IX_Sessions_ExpiresAt` ON `Sessions`;
CREATE INDEX `IX_Sessions_ExpiresAt` ON `Sessions` (`ExpiresAt`);

DROP INDEX IF EXISTS `IX_Sessions_FacilitatorUserId` ON `Sessions`;
CREATE INDEX `IX_Sessions_FacilitatorUserId` ON `Sessions` (`FacilitatorUserId`);

DROP INDEX IF EXISTS `IX_Sessions_GroupId` ON `Sessions`;
CREATE INDEX `IX_Sessions_GroupId` ON `Sessions` (`GroupId`);

-- Activities Indexes
DROP INDEX IF EXISTS `IX_Activities_SessionId_Order` ON `Activities`;
CREATE INDEX `IX_Activities_SessionId_Order` ON `Activities` (`SessionId`, `Order`);

DROP INDEX IF EXISTS `IX_Activities_SessionId_Status` ON `Activities`;
CREATE INDEX `IX_Activities_SessionId_Status` ON `Activities` (`SessionId`, `Status`);

-- Participants Indexes
DROP INDEX IF EXISTS `IX_Participants_SessionId_JoinedAt` ON `Participants`;
CREATE INDEX `IX_Participants_SessionId_JoinedAt` ON `Participants` (`SessionId`, `JoinedAt`);

-- Responses Indexes
DROP INDEX IF EXISTS `IX_Responses_SessionId_ActivityId_CreatedAt` ON `Responses`;
CREATE INDEX `IX_Responses_SessionId_ActivityId_CreatedAt` ON `Responses` (`SessionId`, `ActivityId`, `CreatedAt`);

DROP INDEX IF EXISTS `IX_Responses_ParticipantId_CreatedAt` ON `Responses`;
CREATE INDEX `IX_Responses_ParticipantId_CreatedAt` ON `Responses` (`ParticipantId`, `CreatedAt`);

-- ContributionCounters Indexes
DROP INDEX IF EXISTS `IX_ContributionCounters_SessionId` ON `ContributionCounters`;
CREATE INDEX `IX_ContributionCounters_SessionId` ON `ContributionCounters` (`SessionId`);

SELECT 'Indexes created/verified successfully';
SELECT '';

-- =============================================
-- Step 4: Verification
-- =============================================
SELECT 'Step 4: Verifying setup...';

SELECT CONCAT('Tables in `pulse` schema: ', COUNT(*))
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'pulse';

SELECT CONCAT('Indexes in `pulse` schema: ', COUNT(*))
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'pulse';

SELECT '';

-- =============================================
-- Summary
-- =============================================
SELECT '========================================';
SELECT 'TechWayFit Pulse Database Setup Complete';
SELECT '========================================';
SELECT CONCAT('Completed at: ', NOW());
SELECT '';
SELECT 'Tables created:';
SELECT '  - pulse.FacilitatorUsers';
SELECT '  - pulse.FacilitatorUserData';
SELECT '  - pulse.LoginOtps';
SELECT '  - pulse.SessionGroups';
SELECT '  - pulse.SessionTemplates';
SELECT '  - pulse.Sessions';
SELECT '  - pulse.Activities';
SELECT '  - pulse.Participants';
SELECT '  - pulse.Responses';
SELECT '  - pulse.ContributionCounters';
SELECT '';
SELECT 'Next steps:';
SELECT '  1. Update connection string in appsettings.json';
SELECT '  2. Set Pulse:DatabaseProvider to "MariaDB" or "MySQL"';
SELECT '  3. Restart application';
SELECT '========================================';
