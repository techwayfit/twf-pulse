-- ================================================================
-- TechWayFit Pulse - V1.2 Migration
-- Add Flexible Plan-Based Access Control to Activity Types
-- Database: MariaDB / MySQL
-- Created: March 2026
-- ================================================================

USE pulse;

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;

SELECT '================================================================' AS '';
SELECT 'Starting V1.2 Migration: Flexible Activity Type Access Control' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';
SELECT '' AS '';

-- ================================================================
-- Add ApplicablePlanIds and IsAvailableToAllPlans columns
-- Remove deprecated MinPlanCode column
-- ================================================================

-- Add new columns
ALTER TABLE `ActivityTypeDefinitions`
ADD COLUMN `ApplicablePlanIds` VARCHAR(500) NULL AFTER `RequiresPremium`,
ADD COLUMN `IsAvailableToAllPlans` TINYINT(1) NOT NULL DEFAULT 0 AFTER `ApplicablePlanIds`;

SELECT '? ApplicablePlanIds and IsAvailableToAllPlans columns added' AS '';

-- Drop deprecated MinPlanCode column and its foreign key
ALTER TABLE `ActivityTypeDefinitions`
DROP FOREIGN KEY `FK_ActivityTypeDefinitions_MinPlanCode`;

ALTER TABLE `ActivityTypeDefinitions`
DROP COLUMN `MinPlanCode`;

SELECT '? Deprecated MinPlanCode column removed' AS '';

-- ================================================================
-- Update existing data
-- ================================================================

-- Free activities (available to all plans)
UPDATE `ActivityTypeDefinitions` 
SET `IsAvailableToAllPlans` = 1,
    `ApplicablePlanIds` = NULL
WHERE `RequiresPremium` = 0;

SELECT CONCAT('? Updated ', ROW_COUNT(), ' free activities (available to all plans)') AS '';

-- Premium activities (Five Whys and AI Summary - available to plan-a and plan-b)
UPDATE `ActivityTypeDefinitions` 
SET `IsAvailableToAllPlans` = 0,
    `ApplicablePlanIds` = '00000000-0000-0000-0000-000000000002|00000000-0000-0000-0000-000000000003',
    `UpdatedAt` = UTC_TIMESTAMP(6)
WHERE `ActivityType` IN (6, 8); -- FiveWhys (6), AI Summary (8)

SELECT CONCAT('? Updated ', ROW_COUNT(), ' premium activities (Plan A and Plan B access)') AS '';

-- ================================================================
-- Verification
-- ================================================================

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Verification Report' AS '';
SELECT '================================================================' AS '';

SELECT 
    DisplayName,
    ActivityType,
    RequiresPremium,
    IsAvailableToAllPlans,
    CASE 
        WHEN ApplicablePlanIds IS NULL THEN 'All Plans'
        ELSE CONCAT(LENGTH(ApplicablePlanIds) - LENGTH(REPLACE(ApplicablePlanIds, '|', '')) + 1, ' plans')
    END AS PlanCount,
    ApplicablePlanIds
FROM ActivityTypeDefinitions
WHERE IsActive = 1
ORDER BY SortOrder;

SELECT '' AS '';
SELECT 'Summary:' AS '';
SELECT CONCAT('  - Free activities: ', COUNT(*)) AS '' 
FROM ActivityTypeDefinitions 
WHERE IsAvailableToAllPlans = 1 AND IsActive = 1;

SELECT CONCAT('  - Premium activities: ', COUNT(*)) AS '' 
FROM ActivityTypeDefinitions 
WHERE IsAvailableToAllPlans = 0 AND IsActive = 1;

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Migration V1.2 Completed Successfully' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
