-- =============================================
-- TechWayFit Pulse Database Schema - Version 1.0
-- SQL Server Setup Script
-- =============================================
-- Description: Creates the pulse schema for TechWayFit Pulse application
-- Author: TechWayFit Development Team
-- Date: January 2026
-- =============================================

-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pulse')
BEGIN
    EXEC('CREATE SCHEMA pulse');
    PRINT 'Schema [pulse] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [pulse] already exists';
END
GO
