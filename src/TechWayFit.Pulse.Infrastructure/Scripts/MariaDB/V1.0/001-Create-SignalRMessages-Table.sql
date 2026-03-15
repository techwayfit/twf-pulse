-- =====================================================
-- SignalR Database Backplane Migration - MariaDB/MySQL
-- Version: 1.0.0
-- Date: 2026-03-15
-- Description: Creates SignalRMessages table for cross-server SignalR communication
-- =====================================================

-- Create SignalRMessages table
CREATE TABLE IF NOT EXISTS `SignalRMessages` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `GroupName` VARCHAR(200) NOT NULL COMMENT 'SignalR group name (e.g., SESSION_ABC-DEF-GHI)',
    `MethodName` VARCHAR(100) NOT NULL COMMENT 'Hub method name (e.g., ResponseReceived)',
    `PayloadJson` MEDIUMTEXT NOT NULL COMMENT 'JSON-serialized message arguments',
  `ServerId` VARCHAR(50) NOT NULL COMMENT 'Unique server identifier',
    `CreatedAt` DATETIME(6) NOT NULL COMMENT 'Message creation timestamp',
  `IsProcessed` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether this server has processed the message',
    `ProcessedAt` DATETIME(6) NULL COMMENT 'When the message was processed',
  
    PRIMARY KEY (`Id`),
    INDEX `IX_SignalRMessages_Processing` (`IsProcessed`, `CreatedAt`),
    INDEX `IX_SignalRMessages_GroupName` (`GroupName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='SignalR messages for cross-server communication in web farm deployments';

-- Verify table creation
SELECT 
    TABLE_NAME,
    ENGINE,
  TABLE_ROWS,
    AVG_ROW_LENGTH,
    DATA_LENGTH,
    INDEX_LENGTH,
    CREATE_TIME,
 TABLE_COMMENT
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'SignalRMessages';

-- Show indexes
SHOW INDEX FROM `SignalRMessages`;

-- Show table structure
DESCRIBE `SignalRMessages`;
