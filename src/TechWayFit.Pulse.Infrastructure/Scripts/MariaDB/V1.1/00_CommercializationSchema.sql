-- ================================================================
-- TechWayFit Pulse - Commercialization Schema
-- Version: 1.1 - Subscription Plans & Activity Type Definitions
-- Database: MariaDB / MySQL
-- Created: March 2026
-- ================================================================

USE pulse;

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;

SELECT '================================================================' AS '';
SELECT 'Starting V1.1 Migration: Commercialization Schema' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';
SELECT '' AS '';

-- ================================================================
-- TABLE 1: SubscriptionPlans
-- Defines pricing tiers with quota limits and feature flags
-- ================================================================

CREATE TABLE IF NOT EXISTS `SubscriptionPlans` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `PlanCode` VARCHAR(50) NOT NULL,
    `DisplayName` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `PriceMonthly` DECIMAL(10,2) NOT NULL,
    `PriceYearly` DECIMAL(10,2) NULL,
    `MaxSessionsPerMonth` INT NOT NULL,
    `FeaturesJson` LONGTEXT NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `SortOrder` INT NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL,
    `UpdatedAt` DATETIME(6) NOT NULL,
    
    UNIQUE KEY `UQ_SubscriptionPlans_PlanCode` (`PlanCode`),
    KEY `IX_SubscriptionPlans_IsActive_SortOrder` (`IsActive`, `SortOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT '? SubscriptionPlans table created/verified' AS '';

-- ================================================================
-- TABLE 2: FacilitatorSubscriptions
-- Tracks user subscriptions (current and historical)
-- ================================================================

CREATE TABLE IF NOT EXISTS `FacilitatorSubscriptions` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `FacilitatorUserId` CHAR(36) NOT NULL,
    `PlanId` CHAR(36) NOT NULL,
    `Status` VARCHAR(20) NOT NULL, -- Active, Canceled, Expired, Trial
    `StartsAt` DATETIME(6) NOT NULL,
    `ExpiresAt` DATETIME(6) NULL,
    `CanceledAt` DATETIME(6) NULL,
    `SessionsUsed` INT NOT NULL DEFAULT 0,
    `SessionsResetAt` DATETIME(6) NOT NULL,
    `PaymentProvider` VARCHAR(50) NULL,
    `ExternalCustomerId` VARCHAR(200) NULL,
    `ExternalSubscriptionId` VARCHAR(200) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `UpdatedAt` DATETIME(6) NOT NULL,
    
    KEY `IX_FacilitatorSubscriptions_UserId_Status` (`FacilitatorUserId`, `Status`),
    KEY `IX_FacilitatorSubscriptions_ExternalSubscriptionId` (`ExternalSubscriptionId`),
  KEY `IX_FacilitatorSubscriptions_PlanId` (`PlanId`),
    
    CONSTRAINT `FK_FacilitatorSubscriptions_FacilitatorUsers`
      FOREIGN KEY (`FacilitatorUserId`)
 REFERENCES `FacilitatorUsers`(`Id`)
    ON DELETE CASCADE,
    
    CONSTRAINT `FK_FacilitatorSubscriptions_SubscriptionPlans`
        FOREIGN KEY (`PlanId`)
        REFERENCES `SubscriptionPlans`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT '? FacilitatorSubscriptions table created/verified' AS '';

-- ================================================================
-- TABLE 3: ActivityTypeDefinitions
-- Metadata and access rules for activity types
-- ================================================================

CREATE TABLE IF NOT EXISTS `ActivityTypeDefinitions` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `ActivityType` INT NOT NULL,
    `DisplayName` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(500) NOT NULL,
    `IconClass` VARCHAR(100) NOT NULL,
    `ColorHex` VARCHAR(7) NOT NULL,
`RequiresPremium` TINYINT(1) NOT NULL DEFAULT 0,
    `MinPlanCode` VARCHAR(50) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `SortOrder` INT NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL,
    `UpdatedAt` DATETIME(6) NOT NULL,
    
    UNIQUE KEY `UQ_ActivityTypeDefinitions_ActivityType` (`ActivityType`),
  KEY `IX_ActivityTypeDefinitions_IsActive_SortOrder` (`IsActive`, `SortOrder`),
    
    CONSTRAINT `FK_ActivityTypeDefinitions_MinPlanCode`
        FOREIGN KEY (`MinPlanCode`)
        REFERENCES `SubscriptionPlans`(`PlanCode`)
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT '? ActivityTypeDefinitions table created/verified' AS '';

-- ================================================================
-- SEED DATA: SubscriptionPlans
-- Free, Plan A, Plan B with feature flags
-- ================================================================

SELECT '' AS '';
SELECT 'Seeding SubscriptionPlans...' AS '';

INSERT IGNORE INTO `SubscriptionPlans` VALUES
    ('00000000-0000-0000-0000-000000000001', 'free', 'Free', 
     'Perfect for trying out TechWayFit Pulse with limited sessions',
     0.00, NULL, 2, 
     '{"aiAssist":false,"fiveWhys":false,"aiSummary":false}',
     1, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `SubscriptionPlans` VALUES
    ('00000000-0000-0000-0000-000000000002', 'plan-a', 'Plan A',
 'Ideal for individual facilitators running regular workshops',
     10.00, 100.00, 5,
     '{"aiAssist":true,"fiveWhys":true,"aiSummary":true}',
     1, 2, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `SubscriptionPlans` VALUES
    ('00000000-0000-0000-0000-000000000003', 'plan-b', 'Plan B',
     'Best for teams and frequent facilitators',
     20.00, 200.00, 15,
     '{"aiAssist":true,"fiveWhys":true,"aiSummary":true}',
     1, 3, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

SELECT CONCAT('  ? Seeded ', ROW_COUNT(), ' subscription plans') AS '';

-- ================================================================
-- SEED DATA: ActivityTypeDefinitions
-- Metadata for all implemented activity types
-- ================================================================

SELECT '' AS '';
SELECT 'Seeding ActivityTypeDefinitions...' AS '';

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000001', 0, 'Poll',
   'Multiple choice questions with single or multiple selection',
     'ics ics-chart ic-sm', '#3B82F6', 0, NULL, 1, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000002', 2, 'Word Cloud',
     'Collect words or short phrases from participants',
     'ics ics-thought-balloon ic-sm', '#10B981', 0, NULL, 1, 2, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000003', 5, 'Quadrant',
     'Item scoring with bubble chart visualization',
     'ics ics-chart-increasing ic-sm', '#8B5CF6', 0, NULL, 1, 3, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000004', 6, 'Five Whys',
     'AI-powered root cause analysis for problem-solving',
     'ics ics-question ic-sm', '#F59E0B', 1, 'plan-a', 1, 4, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000005', 4, 'Rating',
     'Star or numeric ratings with optional comments',
     'ics ics-star ic-sm', '#EF4444', 0, NULL, 1, 5, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000006', 7, 'Feedback',
     'Open-ended feedback collection from participants',
     'ics ics-chat ic-sm', '#06B6D4', 0, NULL, 1, 6, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000007', 3, 'Q&A',
   'Live Q&A with upvoting and moderation',
     'fas fa-lightbulb ic-sm', '#F97316', 0, NULL, 1, 7, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000008', 8, 'AI Summary',
     'AI-generated comprehensive session summary',
   'fas fa-robot ic-sm', '#EC4899', 1, 'plan-a', 1, 8, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

INSERT IGNORE INTO `ActivityTypeDefinitions` VALUES
    ('10000000-0000-0000-0000-000000000009', 9, 'Break',
     'Timed break with countdown and ready signal',
     'fas fa-coffee', '#6B7280', 0, NULL, 1, 9, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

SELECT '  ? Seeded activity type definitions' AS '';

-- ================================================================
-- VERIFICATION
-- ================================================================

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Verification Report' AS '';
SELECT '================================================================' AS '';

SELECT 'SubscriptionPlans' AS `Table`, COUNT(*) AS `Rows` FROM `SubscriptionPlans`
UNION ALL
SELECT 'FacilitatorSubscriptions', COUNT(*) FROM `FacilitatorSubscriptions`
UNION ALL
SELECT 'ActivityTypeDefinitions', COUNT(*) FROM `ActivityTypeDefinitions`;

SELECT '' AS '';
SELECT 'Expected:' AS '';
SELECT '  - SubscriptionPlans: 3 rows' AS '';
SELECT '  - FacilitatorSubscriptions: 0 rows (auto-populates on user interaction)' AS '';
SELECT '  - ActivityTypeDefinitions: 9 rows' AS '';

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Migration V1.1 Completed Successfully' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
