-- =============================================
-- TechWayFit Pulse BackOffice - Master Setup Script (MariaDB/MySQL)
-- Version 1.0
-- =============================================
-- Description: Creates all BackOffice-exclusive tables and indexes in the
--              `pulse` database for MariaDB/MySQL.
-- Prerequisites:
--   - MariaDB 10.3+ or MySQL 8.0+
--   - The main application's MariaDB V1.0 setup must have been run first
--     so the `pulse` database and all main-app tables already exist.
--   - Appropriate permissions (CREATE TABLE, CREATE INDEX on `pulse`)
-- Usage:
--   mysql -u <user> -p < 00_MasterSetup.sql
--   or source this file from a MySQL/MariaDB client session.
-- =============================================

SELECT '========================================';
SELECT 'TechWayFit Pulse BackOffice Setup v1.0';
SELECT '========================================';
SELECT CONCAT('Started at: ', NOW());
SELECT '';

-- =============================================
-- Step 1: Verify pulse database exists
-- =============================================
SELECT 'Step 1: Verifying `pulse` database...';

-- Fail early if the main-app database has not been created yet
SELECT CONCAT('`pulse` database found: ',
    IF(SCHEMA_NAME IS NOT NULL, 'YES - OK', 'NO - run main-app setup first'))
FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME = 'pulse';

USE `pulse`;

-- =============================================
-- Step 2: Create BackOffice tables
-- =============================================
SELECT '';
SELECT 'Step 2: Creating BackOffice tables...';

SOURCE 01_CreateTables.sql

-- =============================================
-- Step 3: Create BackOffice indexes
-- =============================================
SELECT '';
SELECT 'Step 3: Creating BackOffice indexes...';

SOURCE 02_CreateIndexes.sql

-- =============================================
-- Step 4: Verification
-- =============================================
SELECT '';
SELECT 'Step 4: Verifying setup...';

SELECT CONCAT('BackOffice tables in `pulse`: ', COUNT(*))
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'pulse'
  AND TABLE_NAME IN ('BackOfficeUsers', 'AuditLogs');

SELECT CONCAT('BackOffice indexes in `pulse`: ', COUNT(*))
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'pulse'
  AND TABLE_NAME IN ('BackOfficeUsers', 'AuditLogs')
  AND INDEX_NAME != 'PRIMARY';

-- =============================================
-- Complete
-- =============================================
SELECT '';
SELECT '========================================';
SELECT 'BackOffice setup completed successfully.';
SELECT '========================================';
SELECT CONCAT('Completed at: ', NOW());
SELECT '';
SELECT 'Tables created:';
SELECT '  - pulse.BackOfficeUsers';
SELECT '  - pulse.AuditLogs';
SELECT '';
SELECT 'Next steps:';
SELECT '  1. Set Pulse:DatabaseProvider to "MariaDB" in appsettings.json';
SELECT '  2. Set the PulseDb connection string (see appsettings.MariaDB.json.example)';
SELECT '  3. Set BackOffice:SeedAdminUsername and BackOffice:SeedAdminPassword';
SELECT '  4. Restart the BackOffice application';
SELECT '========================================';
