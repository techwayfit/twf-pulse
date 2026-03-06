-- ================================================================
-- TechWayFit Pulse - Commercialization Schema ROLLBACK
-- Version: 1.1
-- Database: MariaDB / MySQL
-- ================================================================

USE pulse;

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;

SELECT '================================================================' AS '';
SELECT 'Starting V1.1 ROLLBACK: Commercialization Schema' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';
SELECT '' AS '';
SELECT 'WARNING: This will delete all subscription and activity type data!' AS '';
SELECT 'Press Ctrl+C within 5 seconds to cancel...' AS '';
SELECT '' AS '';

-- Wait 5 seconds (MariaDB doesn't have WAITFOR, use DO SLEEP instead)
DO SLEEP(5);

-- ================================================================
-- DROP TABLES (reverse order due to FK constraints)
-- ================================================================

DROP TABLE IF EXISTS `ActivityTypeDefinitions`;
SELECT '? ActivityTypeDefinitions dropped' AS '';

DROP TABLE IF EXISTS `FacilitatorSubscriptions`;
SELECT '? FacilitatorSubscriptions dropped' AS '';

DROP TABLE IF EXISTS `SubscriptionPlans`;
SELECT '? SubscriptionPlans dropped' AS '';

-- ================================================================
-- VERIFICATION
-- ================================================================

SELECT '' AS '';
SELECT '================================================================' AS '';
SELECT 'Rollback V1.1 Completed' AS '';
SELECT CONCAT('Date: ', NOW()) AS '';
SELECT '================================================================' AS '';

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
