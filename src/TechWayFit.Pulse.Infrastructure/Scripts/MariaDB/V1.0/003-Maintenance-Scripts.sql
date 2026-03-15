-- =====================================================
-- SignalR Backplane Maintenance Scripts - MariaDB/MySQL
-- Version: 1.0.0
-- Date: 2026-03-15
-- Description: Maintenance and cleanup operations for SignalR backplane
-- =====================================================

-- ==========================================
-- 1. MANUAL CLEANUP (DELETE OLD MESSAGES)
-- ==========================================
-- Manually trigger cleanup of old messages (normally done automatically by background service)
DELETE FROM SignalRMessages
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);

SELECT ROW_COUNT() as DeletedRows, NOW() as CleanupTime;

-- ==========================================
-- 2. AGGRESSIVE CLEANUP (DELETE MESSAGES > 2 MIN)
-- ==========================================
-- Use this if table is growing too large - deletes messages older than 2 minutes
DELETE FROM SignalRMessages
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 2 MINUTE);

SELECT ROW_COUNT() as DeletedRows, NOW() as CleanupTime;

-- ==========================================
-- 3. TRUNCATE TABLE (EMERGENCY CLEANUP)
-- ==========================================
-- WARNING: This will delete ALL messages. Only use if backplane is stuck.
-- Uncomment to use:
-- TRUNCATE TABLE SignalRMessages;
-- SELECT 'All messages deleted' as Result;

-- ==========================================
-- 4. DELETE SPECIFIC SERVER'S MESSAGES
-- ==========================================
-- Use this to remove messages from a specific server (e.g., if server crashed)
-- Replace 'SERVER_ID_HERE' with actual ServerId from monitoring queries
/*
DELETE FROM SignalRMessages
WHERE ServerId = 'SERVER_ID_HERE'
AND CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);

SELECT ROW_COUNT() as DeletedRows;
*/

-- ==========================================
-- 5. MARK STUCK MESSAGES AS PROCESSED
-- ==========================================
-- Mark messages older than 1 minute as processed (if backplane is stuck)
UPDATE SignalRMessages
SET IsProcessed = 1,
    ProcessedAt = NOW()
WHERE IsProcessed = 0
AND CreatedAt < DATE_SUB(NOW(), INTERVAL 1 MINUTE);

SELECT ROW_COUNT() as MarkedAsProcessed, NOW() as UpdateTime;

-- ==========================================
-- 6. OPTIMIZE TABLE (RECLAIM SPACE)
-- ==========================================
-- Defragments the table and reclaims unused space
OPTIMIZE TABLE SignalRMessages;

-- ==========================================
-- 7. ANALYZE TABLE (UPDATE STATISTICS)
-- ==========================================
-- Updates table statistics for query optimizer
ANALYZE TABLE SignalRMessages;

-- ==========================================
-- 8. CHECK TABLE FOR CORRUPTION
-- ==========================================
-- Checks table integrity
CHECK TABLE SignalRMessages;

-- ==========================================
-- 9. REPAIR TABLE (IF CORRUPTED)
-- ==========================================
-- Only use if CHECK TABLE reports corruption
-- REPAIR TABLE SignalRMessages;

-- ==========================================
-- 10. REBUILD INDEXES
-- ==========================================
-- Recreates indexes (use if query performance degrades)
ALTER TABLE SignalRMessages DROP INDEX IX_SignalRMessages_Processing;
ALTER TABLE SignalRMessages ADD INDEX IX_SignalRMessages_Processing (IsProcessed, CreatedAt);

ALTER TABLE SignalRMessages DROP INDEX IX_SignalRMessages_GroupName;
ALTER TABLE SignalRMessages ADD INDEX IX_SignalRMessages_GroupName (GroupName);

SELECT 'Indexes rebuilt successfully' as Result;

-- ==========================================
-- 11. ADD ADDITIONAL INDEX (OPTIONAL)
-- ==========================================
-- Add index for ServerId queries if you frequently query by server
-- ALTER TABLE SignalRMessages ADD INDEX IX_SignalRMessages_ServerId (ServerId, CreatedAt);

-- ==========================================
-- 12. BACKUP TABLE (BEFORE MAINTENANCE)
-- ==========================================
-- Create a backup before performing major maintenance
-- CREATE TABLE SignalRMessages_Backup AS SELECT * FROM SignalRMessages;
-- SELECT COUNT(*) as BackupRowCount FROM SignalRMessages_Backup;

-- ==========================================
-- 13. RESTORE FROM BACKUP
-- ==========================================
-- Restore table from backup (only if something went wrong)
-- TRUNCATE TABLE SignalRMessages;
-- INSERT INTO SignalRMessages SELECT * FROM SignalRMessages_Backup;
-- DROP TABLE SignalRMessages_Backup;
-- SELECT COUNT(*) as RestoredRowCount FROM SignalRMessages;

-- ==========================================
-- 14. SCHEDULED CLEANUP (CREATE EVENT)
-- ==========================================
-- Create a MySQL event to automatically cleanup old messages every 5 minutes
-- Note: Requires EVENT scheduler to be enabled (SET GLOBAL event_scheduler = ON;)
/*
DELIMITER $$

CREATE EVENT IF NOT EXISTS evt_cleanup_signalr_messages
ON SCHEDULE EVERY 5 MINUTE
DO
BEGIN
    DELETE FROM SignalRMessages
    WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);
    
    -- Optional: Log cleanup
    -- INSERT INTO MaintenanceLog (Action, RowsAffected, ExecutedAt) 
    -- VALUES ('SignalR Cleanup', ROW_COUNT(), NOW());
END$$

DELIMITER ;

-- Show scheduled events
SHOW EVENTS WHERE Name = 'evt_cleanup_signalr_messages';
*/

-- ==========================================
-- 15. DISABLE SCHEDULED CLEANUP
-- ==========================================
-- DROP EVENT IF EXISTS evt_cleanup_signalr_messages;
