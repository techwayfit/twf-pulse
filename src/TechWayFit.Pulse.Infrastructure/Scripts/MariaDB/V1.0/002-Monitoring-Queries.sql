-- =====================================================
-- SignalR Backplane Monitoring Queries - MariaDB/MySQL
-- Version: 1.0.0
-- Date: 2026-03-15
-- Description: Queries for monitoring SignalR database backplane health
-- =====================================================

-- ==========================================
-- 1. CHECK PENDING MESSAGES (QUEUE SIZE)
-- ==========================================
-- Shows how many messages are waiting to be processed
SELECT 
    COUNT(*) as PendingMessages,
    MIN(CreatedAt) as OldestMessage,
    MAX(CreatedAt) as NewestMessage,
    TIMESTAMPDIFF(SECOND, MIN(CreatedAt), NOW()) as OldestMessageAgeSeconds
FROM SignalRMessages
WHERE IsProcessed = 0;

-- ==========================================
-- 2. MESSAGES PER SERVER (LAST 5 MINUTES)
-- ==========================================
-- Shows activity breakdown by server
SELECT 
    ServerId,
 COUNT(*) as MessageCount,
    COUNT(CASE WHEN IsProcessed = 1 THEN 1 END) as ProcessedCount,
    COUNT(CASE WHEN IsProcessed = 0 THEN 1 END) as PendingCount,
    MIN(CreatedAt) as FirstMessage,
    MAX(CreatedAt) as LastMessage
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)
GROUP BY ServerId
ORDER BY MessageCount DESC;

-- ==========================================
-- 3. MESSAGE THROUGHPUT (MESSAGES PER MINUTE)
-- ==========================================
-- Shows message volume over time
SELECT 
  DATE_FORMAT(CreatedAt, '%Y-%m-%d %H:%i:00') as Minute,
    COUNT(*) as MessageCount,
    COUNT(DISTINCT ServerId) as ActiveServers,
  COUNT(DISTINCT GroupName) as ActiveSessions
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY DATE_FORMAT(CreatedAt, '%Y-%m-%d %H:%i:00')
ORDER BY Minute DESC
LIMIT 60;

-- ==========================================
-- 4. TOP MESSAGE TYPES (BY METHOD)
-- ==========================================
-- Shows which hub methods are most active
SELECT 
    MethodName,
    COUNT(*) as MessageCount,
    COUNT(DISTINCT GroupName) as UniqueGroups,
    AVG(TIMESTAMPDIFF(SECOND, CreatedAt, IFNULL(ProcessedAt, NOW()))) as AvgProcessingTimeSeconds
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY MethodName
ORDER BY MessageCount DESC;

-- ==========================================
-- 5. TOP GROUPS (BY ACTIVITY)
-- ==========================================
-- Shows which sessions are most active
SELECT 
    GroupName,
    COUNT(*) as MessageCount,
    COUNT(DISTINCT MethodName) as UniqueMethodTypes,
  MIN(CreatedAt) as FirstMessage,
    MAX(CreatedAt) as LastMessage
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY GroupName
ORDER BY MessageCount DESC
LIMIT 20;

-- ==========================================
-- 6. TABLE SIZE AND STATISTICS
-- ==========================================
-- Shows table size and resource usage
SELECT 
    table_name as TableName,
    table_rows as ApproxRowCount,
    ROUND(((data_length + index_length) / 1024 / 1024), 2) AS SizeMB,
    ROUND((data_length / 1024 / 1024), 2) AS DataMB,
    ROUND((index_length / 1024 / 1024), 2) AS IndexMB,
    ROUND((index_length / data_length * 100), 2) AS IndexRatio,
    auto_increment as NextId
FROM information_schema.TABLES 
WHERE table_schema = DATABASE()
AND table_name = 'SignalRMessages';

-- ==========================================
-- 7. PROCESSING LATENCY (AVG TIME TO PROCESS)
-- ==========================================
-- Shows cross-server message latency
-- NOTE: MariaDB doesn't support TIMESTAMPDIFF(MILLISECOND), use MICROSECOND/1000 instead
SELECT 
    ServerId as ProcessingServer,
    COUNT(*) as ProcessedMessages,
    ROUND(AVG(TIMESTAMPDIFF(MICROSECOND, CreatedAt, ProcessedAt) / 1000), 0) as AvgLatencyMs,
 ROUND(MIN(TIMESTAMPDIFF(MICROSECOND, CreatedAt, ProcessedAt) / 1000), 0) as MinLatencyMs,
    ROUND(MAX(TIMESTAMPDIFF(MICROSECOND, CreatedAt, ProcessedAt) / 1000), 0) as MaxLatencyMs
FROM SignalRMessages
WHERE IsProcessed = 1
AND ProcessedAt IS NOT NULL
AND CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY ServerId
ORDER BY AvgLatencyMs DESC;

-- ==========================================
-- 8. RECENT ERRORS (UNPROCESSED OLD MESSAGES)
-- ==========================================
-- Shows messages that are stuck (should be processed within 30 seconds)
SELECT 
    Id,
    GroupName,
    MethodName,
    ServerId,
  CreatedAt,
    TIMESTAMPDIFF(SECOND, CreatedAt, NOW()) as AgeSeconds
FROM SignalRMessages
WHERE IsProcessed = 0
AND CreatedAt < DATE_SUB(NOW(), INTERVAL 30 SECOND)
ORDER BY CreatedAt ASC
LIMIT 100;

-- ==========================================
-- 9. ACTIVE SERVERS (LAST 5 MINUTES)
-- ==========================================
-- Shows which servers are currently active
SELECT DISTINCT 
    ServerId,
    COUNT(*) as MessageCount,
    MAX(CreatedAt) as LastActivity,
    TIMESTAMPDIFF(SECOND, MAX(CreatedAt), NOW()) as SecondsSinceLastMessage,
    CASE 
    WHEN MAX(CreatedAt) > DATE_SUB(NOW(), INTERVAL 30 SECOND) THEN 'ACTIVE'
        WHEN MAX(CreatedAt) > DATE_SUB(NOW(), INTERVAL 2 MINUTE) THEN 'IDLE'
  ELSE 'OFFLINE'
    END as Status
FROM SignalRMessages
WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)
GROUP BY ServerId
ORDER BY LastActivity DESC;

-- ==========================================
-- 10. CLEANUP CANDIDATES (OLD MESSAGES)
-- ==========================================
-- Shows how many messages are ready for cleanup
SELECT 
    COUNT(*) as OldMessageCount,
    MIN(CreatedAt) as OldestMessage,
    MAX(CreatedAt) as NewestOldMessage,
    ROUND(SUM(LENGTH(PayloadJson)) / 1024 / 1024, 2) as PayloadSizeMB
FROM SignalRMessages
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 5 MINUTE);

-- ==========================================
-- 11. REAL-TIME DASHBOARD (REFRESH EVERY 5 SEC)
-- ==========================================
-- Single query that shows overall backplane health
SELECT 
    (SELECT COUNT(*) FROM SignalRMessages WHERE IsProcessed = 0) as PendingMessages,
 (SELECT COUNT(*) FROM SignalRMessages WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 1 MINUTE)) as LastMinute,
    (SELECT COUNT(DISTINCT ServerId) FROM SignalRMessages WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)) as ActiveServers,
    (SELECT COUNT(DISTINCT GroupName) FROM SignalRMessages WHERE CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)) as ActiveSessions,
    (SELECT ROUND(AVG(TIMESTAMPDIFF(MICROSECOND, CreatedAt, ProcessedAt) / 1000), 0) 
     FROM SignalRMessages 
     WHERE IsProcessed = 1 AND ProcessedAt IS NOT NULL AND CreatedAt > DATE_SUB(NOW(), INTERVAL 5 MINUTE)) as AvgLatencyMs,
    (SELECT COUNT(*) FROM SignalRMessages) as TotalMessages,
    (SELECT ROUND(((data_length + index_length) / 1024 / 1024), 2) 
   FROM information_schema.TABLES 
     WHERE table_schema = DATABASE() AND table_name = 'SignalRMessages') as TableSizeMB;
