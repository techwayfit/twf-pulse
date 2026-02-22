-- =============================================
-- TechWayFit Pulse BackOffice - Master Setup Script
-- Version 1.0
-- =============================================
-- Description: Executes all BackOffice schema scripts in order.
-- Prerequisite: The main application's V1.0 scripts must have been run first
--               so the [pulse] schema and all main-app tables already exist.
-- Usage: Run this script against your SQL Server database as a DBA or
--        db_owner. Individual scripts can also be run separately.
-- =============================================

USE [TechWayFitPulse]
GO

PRINT '========================================';
PRINT 'TechWayFit Pulse BackOffice Setup v1.0';
PRINT '========================================';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
GO

-- =============================================
-- Step 1: Ensure pulse schema exists
-- =============================================
PRINT 'Step 1: Verifying [pulse] schema...';
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pulse')
BEGIN
    EXEC('CREATE SCHEMA pulse');
    PRINT 'Schema [pulse] created';
END
ELSE
BEGIN
    PRINT 'Schema [pulse] already exists - OK';
END
GO

-- =============================================
-- Step 2: Create BackOffice tables
-- =============================================
PRINT '';
PRINT 'Step 2: Creating BackOffice tables...';
GO

:r 01_CreateTables.sql

-- =============================================
-- Step 3: Create BackOffice indexes
-- =============================================
PRINT '';
PRINT 'Step 3: Creating BackOffice indexes...';
GO

:r 02_CreateIndexes.sql

-- =============================================
-- Complete
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'BackOffice setup completed successfully.';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================';
GO
