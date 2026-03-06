-- ================================================================
-- TechWayFit Pulse - Promo Codes Feature
-- Version: 1.3 - Promotional Code Management
-- Database: MariaDB / MySQL
-- Created: January 2025
-- ================================================================

USE pulse;

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;

SELECT '================================================================' AS '';
SELECT 'Starting V1.3 Migration: Promo Codes Feature' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';
SELECT '' AS '';

-- ================================================================
-- TABLE 1: PromoCodes
-- Promotional codes that grant temporary access to plans
-- ================================================================

CREATE TABLE IF NOT EXISTS `PromoCodes` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `Code` VARCHAR(50) NOT NULL,
    `TargetPlanId` CHAR(36) NOT NULL,
    `DurationDays` INT NOT NULL,
    `MaxRedemptions` INT NULL,
    `RedemptionsUsed` INT NOT NULL DEFAULT 0,
    `ValidFrom` DATETIME(6) NOT NULL,
    `ValidUntil` DATETIME(6) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL,
    `UpdatedAt` DATETIME(6) NOT NULL,
 
    UNIQUE KEY `UQ_PromoCodes_Code` (`Code`),
    KEY `IX_PromoCodes_IsActive_ValidDates` (`IsActive`, `ValidFrom`, `ValidUntil`),
    KEY `IX_PromoCodes_TargetPlanId` (`TargetPlanId`),
    
    CONSTRAINT `FK_PromoCodes_SubscriptionPlans`
        FOREIGN KEY (`TargetPlanId`)
     REFERENCES `SubscriptionPlans`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT '? PromoCodes table created/verified' AS '';

-- ================================================================
-- TABLE 2: PromoCodeRedemptions
-- Audit trail of promo code usage
-- ================================================================

CREATE TABLE IF NOT EXISTS `PromoCodeRedemptions` (
  `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `PromoCodeId` CHAR(36) NOT NULL,
    `FacilitatorUserId` CHAR(36) NOT NULL,
    `SubscriptionId` CHAR(36) NOT NULL,
    `RedeemedAt` DATETIME(6) NOT NULL,
    `IpAddress` VARCHAR(45) NOT NULL,
    
    KEY `IX_PromoCodeRedemptions_PromoCodeId_UserId` (`PromoCodeId`, `FacilitatorUserId`),
    KEY `IX_PromoCodeRedemptions_UserId` (`FacilitatorUserId`),
    KEY `IX_PromoCodeRedemptions_SubscriptionId` (`SubscriptionId`),
    KEY `IX_PromoCodeRedemptions_RedeemedAt` (`RedeemedAt`),
    
 CONSTRAINT `FK_PromoCodeRedemptions_PromoCodes`
  FOREIGN KEY (`PromoCodeId`)
        REFERENCES `PromoCodes`(`Id`),
    
    CONSTRAINT `FK_PromoCodeRedemptions_FacilitatorUsers`
    FOREIGN KEY (`FacilitatorUserId`)
        REFERENCES `FacilitatorUsers`(`Id`)
        ON DELETE CASCADE,
    
    CONSTRAINT `FK_PromoCodeRedemptions_Subscriptions`
        FOREIGN KEY (`SubscriptionId`)
  REFERENCES `FacilitatorSubscriptions`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT '? PromoCodeRedemptions table created/verified' AS '';

-- ================================================================
-- SEED DATA: Sample Promo Codes
-- Example codes for testing and initial launch
-- ================================================================

SELECT '' AS '';
SELECT 'Seeding sample promo codes...' AS '';

-- Launch campaign: 30 days Plan A, limited to 100 redemptions
INSERT IGNORE INTO `PromoCodes` VALUES
    (UUID(), 'LAUNCH2025', '00000000-0000-0000-0000-000000000002', 30, 100, 0,
     UTC_TIMESTAMP(6), DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 3 MONTH),
     1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

-- Friends & family: 60 days Plan B, unlimited redemptions
INSERT IGNORE INTO `PromoCodes` VALUES
    (UUID(), 'FRIENDS50', '00000000-0000-0000-0000-000000000003', 60, NULL, 0,
     UTC_TIMESTAMP(6), DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 1 YEAR),
     1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

-- Support comp: 90 days Plan A, limited to 20 redemptions
INSERT IGNORE INTO `PromoCodes` VALUES
    (UUID(), 'SUPPORT2025', '00000000-0000-0000-0000-000000000002', 90, 20, 0,
     UTC_TIMESTAMP(6), DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 6 MONTH),
     1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

SELECT CONCAT('  ? Seeded ', ROW_COUNT(), ' promo codes') AS '';

-- ================================================================
-- VERIFICATION
-- ================================================================

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Verification Report' AS '';
SELECT '================================================================' AS '';

SELECT 'PromoCodes' AS `Table`, COUNT(*) AS `Rows` FROM `PromoCodes`
UNION ALL
SELECT 'PromoCodeRedemptions', COUNT(*) FROM `PromoCodeRedemptions`;

SELECT '' AS '';
SELECT 'Expected:' AS '';
SELECT '  - PromoCodes: 3 rows (sample codes)' AS '';
SELECT '  - PromoCodeRedemptions: 0 rows (populated on user redemption)' AS '';

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Sample Promo Codes:' AS '';
SELECT '================================================================' AS '';

SELECT 
    Code AS 'Code',
    (SELECT DisplayName FROM SubscriptionPlans WHERE Id = TargetPlanId) AS 'Target Plan',
    DurationDays AS 'Duration (Days)',
    COALESCE(MaxRedemptions, 9999) AS 'Max Uses',
    RedemptionsUsed AS 'Used',
    DATE_FORMAT(ValidUntil, '%Y-%m-%d') AS 'Expires'
FROM PromoCodes
ORDER BY Code;

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Migration V1.3 Completed Successfully' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
