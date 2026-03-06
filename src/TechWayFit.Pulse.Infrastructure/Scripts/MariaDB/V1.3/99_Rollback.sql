-- ================================================================
-- TechWayFit Pulse - Rollback V1.3 Migration
-- Version: 1.3 - Remove Promo Code Feature
-- Database: MariaDB / MySQL
-- ================================================================

USE pulse;

SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;

SELECT '================================================================' AS '';
SELECT 'Rolling back V1.3 Migration: Promo Codes Feature' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';
SELECT '' AS '';

-- ?? WARNING: This will delete all promo codes and redemption history
SELECT 'WARNING: This will DELETE all promo code data!' AS '';
SELECT '' AS '';

-- Drop tables in reverse dependency order
DROP TABLE IF EXISTS `PromoCodeRedemptions`;
SELECT '? PromoCodeRedemptions table dropped' AS '';

DROP TABLE IF EXISTS `PromoCodes`;
SELECT '? PromoCodes table dropped' AS '';

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Rollback V1.3 Completed Successfully' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
