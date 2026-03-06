-- ================================================================
-- TechWayFit Pulse - Commercialization Schema
-- Version: 1.1 - Subscription Plans & Activity Type Definitions
-- Database: SQL Server
-- Created: March 2026
-- ================================================================

USE [TechWayFitPulse];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT '================================================================';
PRINT 'Starting V1.1 Migration: Commercialization Schema';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '================================================================';
PRINT '';

-- ================================================================
-- TABLE 1: SubscriptionPlans
-- Defines pricing tiers with quota limits and feature flags
-- ================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[SubscriptionPlans]') AND type = 'U')
BEGIN
    PRINT 'Creating table: SubscriptionPlans...';
 
    CREATE TABLE [pulse].[SubscriptionPlans] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [PlanCode] VARCHAR(50) NOT NULL,
        [DisplayName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
[PriceMonthly] DECIMAL(10,2) NOT NULL,
        [PriceYearly] DECIMAL(10,2) NULL,
        [MaxSessionsPerMonth] INT NOT NULL,
[FeaturesJson] NVARCHAR(MAX) NOT NULL,
[IsActive] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL,
     
        CONSTRAINT [UQ_SubscriptionPlans_PlanCode] UNIQUE ([PlanCode])
    );

    CREATE INDEX [IX_SubscriptionPlans_IsActive_SortOrder] 
      ON [pulse].[SubscriptionPlans]([IsActive], [SortOrder]);

    PRINT '? SubscriptionPlans table created';
END
ELSE
    PRINT '? SubscriptionPlans table already exists';

GO

-- ================================================================
-- TABLE 2: FacilitatorSubscriptions
-- Tracks user subscriptions (current and historical)
-- ================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[FacilitatorSubscriptions]') AND type = 'U')
BEGIN
    PRINT 'Creating table: FacilitatorSubscriptions...';
    
    CREATE TABLE [pulse].[FacilitatorSubscriptions] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [FacilitatorUserId] UNIQUEIDENTIFIER NOT NULL,
        [PlanId] UNIQUEIDENTIFIER NOT NULL,
        [Status] VARCHAR(20) NOT NULL, -- Active, Canceled, Expired, Trial
        [StartsAt] DATETIMEOFFSET NOT NULL,
        [ExpiresAt] DATETIMEOFFSET NULL,
        [CanceledAt] DATETIMEOFFSET NULL,
        [SessionsUsed] INT NOT NULL DEFAULT 0,
   [SessionsResetAt] DATETIMEOFFSET NOT NULL,
     [PaymentProvider] VARCHAR(50) NULL, -- 'paddle', 'stripe', null (operator-assigned)
        [ExternalCustomerId] VARCHAR(200) NULL,
        [ExternalSubscriptionId] VARCHAR(200) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL,
        
        CONSTRAINT [FK_FacilitatorSubscriptions_FacilitatorUsers] 
      FOREIGN KEY ([FacilitatorUserId]) 
        REFERENCES [pulse].[FacilitatorUsers]([Id]) 
ON DELETE CASCADE,
        
        CONSTRAINT [FK_FacilitatorSubscriptions_SubscriptionPlans] 
         FOREIGN KEY ([PlanId]) 
            REFERENCES [pulse].[SubscriptionPlans]([Id])
    );

    CREATE INDEX [IX_FacilitatorSubscriptions_UserId_Status] 
        ON [pulse].[FacilitatorSubscriptions]([FacilitatorUserId], [Status]);
    
    CREATE INDEX [IX_FacilitatorSubscriptions_ExternalSubscriptionId] 
        ON [pulse].[FacilitatorSubscriptions]([ExternalSubscriptionId]);
    
    CREATE INDEX [IX_FacilitatorSubscriptions_PlanId] 
        ON [pulse].[FacilitatorSubscriptions]([PlanId]);

    PRINT '? FacilitatorSubscriptions table created';
END
ELSE
    PRINT '? FacilitatorSubscriptions table already exists';

GO

-- ================================================================
-- TABLE 3: ActivityTypeDefinitions
-- Metadata and access rules for activity types
-- ================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[pulse].[ActivityTypeDefinitions]') AND type = 'U')
BEGIN
    PRINT 'Creating table: ActivityTypeDefinitions...';
    
    CREATE TABLE [pulse].[ActivityTypeDefinitions] (
 [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ActivityType] INT NOT NULL, -- Links to ActivityType enum (0-10)
  [DisplayName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [IconClass] NVARCHAR(100) NOT NULL,
        [ColorHex] VARCHAR(7) NOT NULL, -- #RRGGBB
        [RequiresPremium] BIT NOT NULL DEFAULT 0,
        [MinPlanCode] VARCHAR(50) NULL, -- 'plan-a', 'plan-b', null = free
        [IsActive] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL,
        
        CONSTRAINT [UQ_ActivityTypeDefinitions_ActivityType] UNIQUE ([ActivityType]),
        CONSTRAINT [FK_ActivityTypeDefinitions_MinPlanCode] 
     FOREIGN KEY ([MinPlanCode]) 
            REFERENCES [pulse].[SubscriptionPlans]([PlanCode])
   ON UPDATE CASCADE
  );

    CREATE INDEX [IX_ActivityTypeDefinitions_IsActive_SortOrder] 
        ON [pulse].[ActivityTypeDefinitions]([IsActive], [SortOrder]);

    PRINT '? ActivityTypeDefinitions table created';
END
ELSE
    PRINT '? ActivityTypeDefinitions table already exists';

GO

-- ================================================================
-- SEED DATA: SubscriptionPlans
-- Free, Plan A, Plan B with feature flags
-- ================================================================

PRINT '';
PRINT 'Seeding SubscriptionPlans...';

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
DECLARE @FreePlanId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @PlanAId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000002';
DECLARE @PlanBId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000003';

-- Free Plan (2 sessions/month, no AI features)
IF NOT EXISTS (SELECT 1 FROM [pulse].[SubscriptionPlans] WHERE [PlanCode] = 'free')
BEGIN
    INSERT INTO [pulse].[SubscriptionPlans] VALUES (
        @FreePlanId,
        'free',
        'Free',
        'Perfect for trying out TechWayFit Pulse with limited sessions',
        0.00,
   NULL,
        2, -- 2 sessions per month
  '{"aiAssist":false,"fiveWhys":false,"aiSummary":false}',
  1,
        1,
        @Now,
        @Now
    );
    PRINT '  ? Inserted: Free Plan (2 sessions/month, no AI)';
END
ELSE
    PRINT '  ? Free Plan already exists';

-- Plan A ($10/month, 5 sessions, all AI features)
IF NOT EXISTS (SELECT 1 FROM [pulse].[SubscriptionPlans] WHERE [PlanCode] = 'plan-a')
BEGIN
    INSERT INTO [pulse].[SubscriptionPlans] VALUES (
        @PlanAId,
      'plan-a',
        'Plan A',
        'Ideal for individual facilitators running regular workshops',
        10.00,
        100.00, -- Annual discount: $100/year (save $20)
  5, -- 5 sessions per month
  '{"aiAssist":true,"fiveWhys":true,"aiSummary":true}',
        1,
    2,
        @Now,
    @Now
 );
    PRINT '  ? Inserted: Plan A ($10/month, 5 sessions, AI features)';
END
ELSE
    PRINT '  ? Plan A already exists';

-- Plan B ($20/month, 15 sessions, all AI features)
IF NOT EXISTS (SELECT 1 FROM [pulse].[SubscriptionPlans] WHERE [PlanCode] = 'plan-b')
BEGIN
INSERT INTO [pulse].[SubscriptionPlans] VALUES (
        @PlanBId,
      'plan-b',
        'Plan B',
     'Best for teams and frequent facilitators',
  20.00,
        200.00, -- Annual discount: $200/year (save $40)
        15, -- 15 sessions per month
        '{"aiAssist":true,"fiveWhys":true,"aiSummary":true}',
        1,
        3,
   @Now,
        @Now
    );
    PRINT '  ? Inserted: Plan B ($20/month, 15 sessions, AI features)';
END
ELSE
    PRINT '  ? Plan B already exists';

GO

-- ================================================================
-- SEED DATA: ActivityTypeDefinitions
-- Metadata for all implemented activity types
-- ================================================================

PRINT '';
PRINT 'Seeding ActivityTypeDefinitions...';

DECLARE @Now2 DATETIMEOFFSET = SYSDATETIMEOFFSET();

-- Poll (ActivityType = 0, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 0)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
      '10000000-0000-0000-0000-000000000001',
     0, -- Poll
        'Poll',
        'Multiple choice questions with single or multiple selection',
'ics ics-chart ic-sm',
    '#3B82F6',
        0, -- Not premium
        NULL, -- No minimum plan
     1, -- Active
        1, -- Sort order
        @Now2,
  @Now2
    );
    PRINT '  ? Poll (Free)';
END
ELSE
    PRINT '  ? Poll already exists';

-- WordCloud (ActivityType = 2, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 2)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
        '10000000-0000-0000-0000-000000000002',
        2, -- WordCloud
        'Word Cloud',
        'Collect words or short phrases from participants',
        'ics ics-thought-balloon ic-sm',
        '#10B981',
        0,
        NULL,
        1,
2,
        @Now2,
    @Now2
    );
    PRINT '  ? Word Cloud (Free)';
END
ELSE
    PRINT '  ? Word Cloud already exists';

-- Quadrant (ActivityType = 5, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 5)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
  '10000000-0000-0000-0000-000000000003',
        5, -- Quadrant
        'Quadrant',
'Item scoring with bubble chart visualization',
        'ics ics-chart-increasing ic-sm',
        '#8B5CF6',
        0,
        NULL,
        1,
        3,
        @Now2,
        @Now2
    );
    PRINT '  ? Quadrant (Free)';
END
ELSE
 PRINT '  ? Quadrant already exists';

-- FiveWhys (ActivityType = 6, Premium - Plan A+)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 6)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
        '10000000-0000-0000-0000-000000000004',
        6, -- FiveWhys
        'Five Whys',
    'AI-powered root cause analysis for problem-solving',
   'ics ics-question ic-sm',
        '#F59E0B',
        1, -- Requires premium
        'plan-a', -- Minimum Plan A
      1,
        4,
        @Now2,
   @Now2
    );
  PRINT '  ? Five Whys (Premium - Plan A+)';
END
ELSE
    PRINT '  ? Five Whys already exists';

-- Rating (ActivityType = 4, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 4)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
        '10000000-0000-0000-0000-000000000005',
    4, -- Rating
'Rating',
        'Star or numeric ratings with optional comments',
     'ics ics-star ic-sm',
        '#EF4444',
      0,
   NULL,
        1,
      5,
        @Now2,
        @Now2
    );
    PRINT '  ? Rating (Free)';
END
ELSE
    PRINT '  ? Rating already exists';

-- GeneralFeedback (ActivityType = 7, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 7)
BEGIN
  INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
     '10000000-0000-0000-0000-000000000006',
        7, -- GeneralFeedback
        'Feedback',
        'Open-ended feedback collection from participants',
        'ics ics-chat ic-sm',
        '#06B6D4',
        0,
        NULL,
        1,
        6,
    @Now2,
    @Now2
    );
    PRINT '  ? Feedback (Free)';
END
ELSE
    PRINT '  ? Feedback already exists';

-- QnA (ActivityType = 3, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 3)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
        '10000000-0000-0000-0000-000000000007',
        3, -- QnA
        'Q&A',
        'Live Q&A with upvoting and moderation',
        'fas fa-lightbulb ic-sm',
        '#F97316',
        0,
 NULL,
        1,
        7,
        @Now2,
        @Now2
);
    PRINT '  ? Q&A (Free)';
END
ELSE
    PRINT '  ? Q&A already exists';

-- AiSummary (ActivityType = 8, Premium - Plan A+)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 8)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
        '10000000-0000-0000-0000-000000000008',
        8, -- AiSummary
        'AI Summary',
      'AI-generated comprehensive session summary',
        'fas fa-robot ic-sm',
    '#EC4899',
     1, -- Requires premium
   'plan-a', -- Minimum Plan A
    1,
      8,
        @Now2,
  @Now2
    );
    PRINT '  ? AI Summary (Premium - Plan A+)';
END
ELSE
    PRINT '  ? AI Summary already exists';

-- Break (ActivityType = 9, Free)
IF NOT EXISTS (SELECT 1 FROM [pulse].[ActivityTypeDefinitions] WHERE [ActivityType] = 9)
BEGIN
    INSERT INTO [pulse].[ActivityTypeDefinitions] VALUES (
     '10000000-0000-0000-0000-000000000009',
        9, -- Break
        'Break',
        'Timed break with countdown and ready signal',
      'fas fa-coffee',
        '#6B7280',
        0,
        NULL,
     1,
        9,
        @Now2,
 @Now2
    );
    PRINT '  ? Break (Free)';
END
ELSE
    PRINT '  ? Break already exists';

GO

-- ================================================================
-- MIGRATION HISTORY
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM [pulse].[__MigrationHistory] WHERE [MigrationId] = 'V1.1_Commercialization')
BEGIN
    INSERT INTO [pulse].[__MigrationHistory] ([MigrationId], [AppliedAt])
    VALUES ('V1.1_Commercialization', SYSDATETIMEOFFSET());
    PRINT '';
    PRINT '? Migration V1.1_Commercialization recorded in history';
END

GO

-- ================================================================
-- VERIFICATION
-- ================================================================

PRINT '';
PRINT '================================================================';
PRINT 'Verification Report';
PRINT '================================================================';

SELECT 'SubscriptionPlans' AS [Table], COUNT(*) AS [Rows] FROM [pulse].[SubscriptionPlans]
UNION ALL
SELECT 'FacilitatorSubscriptions', COUNT(*) FROM [pulse].[FacilitatorSubscriptions]
UNION ALL
SELECT 'ActivityTypeDefinitions', COUNT(*) FROM [pulse].[ActivityTypeDefinitions];

PRINT '';
PRINT 'Expected:';
PRINT '  - SubscriptionPlans: 3 rows';
PRINT '  - FacilitatorSubscriptions: 0 rows (will auto-populate on user interaction)';
PRINT '  - ActivityTypeDefinitions: 9 rows';

PRINT '';
PRINT '================================================================';
PRINT 'Migration V1.1 Completed Successfully';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '================================================================';

GO
