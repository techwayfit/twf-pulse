-- ================================================================
-- TechWayFit Pulse - Commercialization Schema ROLLBACK
-- Version: 1.1
-- Database: SQL Server
-- ================================================================

USE [TechWayFitPulse];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT '================================================================';
PRINT 'Starting V1.1 ROLLBACK: Commercialization Schema';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '================================================================';
PRINT '';
PRINT 'WARNING: This will delete all subscription and activity type data!';
PRINT 'Press Ctrl+C within 5 seconds to cancel...';
PRINT '';

WAITFOR DELAY '00:00:05';

-- ================================================================
-- DROP TABLES (reverse order of creation due to FK constraints)
-- ================================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[ActivityTypeDefinitions]') AND type = 'U')
BEGIN
 PRINT 'Dropping table: ActivityTypeDefinitions...';
    DROP TABLE [pulse].[ActivityTypeDefinitions];
    PRINT '? ActivityTypeDefinitions dropped';
END
ELSE
    PRINT '? ActivityTypeDefinitions does not exist';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[FacilitatorSubscriptions]') AND type = 'U')
BEGIN
    PRINT 'Dropping table: FacilitatorSubscriptions...';
    DROP TABLE [pulse].[FacilitatorSubscriptions];
    PRINT '? FacilitatorSubscriptions dropped';
END
ELSE
    PRINT '? FacilitatorSubscriptions does not exist';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[SubscriptionPlans]') AND type = 'U')
BEGIN
    PRINT 'Dropping table: SubscriptionPlans...';
    DROP TABLE [pulse].[SubscriptionPlans];
    PRINT '? SubscriptionPlans dropped';
END
ELSE
    PRINT '? SubscriptionPlans does not exist';

GO

-- ================================================================
-- REMOVE FROM MIGRATION HISTORY
-- ================================================================

IF EXISTS (SELECT 1 FROM [pulse].[__MigrationHistory] WHERE [MigrationId] = 'V1.1_Commercialization')
BEGIN
    DELETE FROM [pulse].[__MigrationHistory] WHERE [MigrationId] = 'V1.1_Commercialization';
    PRINT '';
    PRINT '? V1.1_Commercialization removed from migration history';
END

GO

PRINT '';
PRINT '================================================================';
PRINT 'Rollback V1.1 Completed';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '================================================================';

GO
