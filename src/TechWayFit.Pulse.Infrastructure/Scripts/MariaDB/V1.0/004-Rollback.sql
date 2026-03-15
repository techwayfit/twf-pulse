-- =====================================================
-- SignalR Backplane Rollback Script - MariaDB/MySQL
-- Version: 1.0.0
-- Date: 2026-03-15
-- Description: Removes SignalR database backplane (for single-server deployments)
-- =====================================================

-- WARNING: This will delete all SignalR message data
-- Make sure SignalR:UseDatabaseBackplane is set to false in appsettings.json before running

-- ==========================================
-- 1. BACKUP TABLE (OPTIONAL)
-- ==========================================
-- Uncomment to create a backup before dropping
-- CREATE TABLE SignalRMessages_Backup_BeforeRollback AS SELECT * FROM SignalRMessages;
-- SELECT COUNT(*) as BackupRowCount, NOW() as BackupTime FROM SignalRMessages_Backup_BeforeRollback;

-- ==========================================
-- 2. DROP SCHEDULED EVENT (IF EXISTS)
-- ==========================================
DROP EVENT IF EXISTS evt_cleanup_signalr_messages;

-- ==========================================
-- 3. DROP INDEXES
-- ==========================================
ALTER TABLE SignalRMessages DROP INDEX IF EXISTS IX_SignalRMessages_Processing;
ALTER TABLE SignalRMessages DROP INDEX IF EXISTS IX_SignalRMessages_GroupName;
-- DROP additional custom indexes if you created any
-- ALTER TABLE SignalRMessages DROP INDEX IF EXISTS IX_SignalRMessages_ServerId;

SELECT 'Indexes dropped successfully' as Result;

-- ==========================================
-- 4. DROP TABLE
-- ==========================================
DROP TABLE IF EXISTS SignalRMessages;

SELECT 'SignalRMessages table dropped successfully' as Result;

-- ==========================================
-- 5. VERIFY REMOVAL
-- ==========================================
-- This should return empty result
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'SignalRMessages';

-- If the table still exists, the above query will return a row
-- If rollback was successful, it should return 0 rows

-- ==========================================
-- 6. CLEANUP VERIFICATION
-- ==========================================
SELECT 
    CASE 
        WHEN NOT EXISTS (
    SELECT 1 FROM information_schema.TABLES 
            WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'SignalRMessages'
    ) 
        THEN 'ROLLBACK SUCCESSFUL: SignalRMessages table has been removed'
   ELSE 'ROLLBACK FAILED: SignalRMessages table still exists'
    END as RollbackStatus,
    NOW() as CheckTime;
