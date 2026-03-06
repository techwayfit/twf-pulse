using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Services;

/// <summary>
/// Service that seeds commercialization data (plans and activity types) on startup.
/// Called from EnsurePulseDatabase for SQLite and InMemory providers.
/// For SQL Server and MariaDB, seed data is applied via manual SQL scripts.
/// </summary>
public static class CommercializationSeedService
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<IPulseDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IPulseDbContext>>();

     try
   {
            await SeedSubscriptionPlansAsync(dbContext, logger);
      await SeedActivityTypeDefinitionsAsync(dbContext, logger);
         
            logger.LogInformation("Commercialization seed data initialized successfully");
        }
      catch (Exception ex)
        {
         logger.LogError(ex, "Failed to seed commercialization data");
       // Don't throw - let app start even if seeding fails
     }
    }

    private static async Task SeedSubscriptionPlansAsync(IPulseDbContext dbContext, ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;

        // Check if plans already exist
    var existingPlans = dbContext.SubscriptionPlans.Any();
    if (existingPlans)
        {
 logger.LogDebug("Subscription plans already seeded, skipping");
return;
        }

   logger.LogInformation("Seeding subscription plans...");

        var plans = new List<SubscriptionPlanRecord>
        {
     // Free Plan
new()
       {
          Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
 PlanCode = "free",
  DisplayName = "Free",
      Description = "Perfect for trying out TechWayFit Pulse with limited sessions",
          PriceMonthly = 0.00m,
PriceYearly = null,
       MaxSessionsPerMonth = 2,
                FeaturesJson = "{\"aiAssist\":false,\"fiveWhys\":false,\"aiSummary\":false}",
    IsActive = true,
       SortOrder = 1,
              CreatedAt = now,
         UpdatedAt = now
            },
         // Plan A
            new()
    {
      Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
         PlanCode = "plan-a",
   DisplayName = "Plan A",
Description = "Ideal for individual facilitators running regular workshops",
    PriceMonthly = 10.00m,
        PriceYearly = 100.00m,
        MaxSessionsPerMonth = 5,
     FeaturesJson = "{\"aiAssist\":true,\"fiveWhys\":true,\"aiSummary\":true}",
        IsActive = true,
           SortOrder = 2,
           CreatedAt = now,
          UpdatedAt = now
   },
            // Plan B
            new()
            {
          Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
        PlanCode = "plan-b",
         DisplayName = "Plan B",
   Description = "Best for teams and frequent facilitators",
     PriceMonthly = 20.00m,
              PriceYearly = 200.00m,
  MaxSessionsPerMonth = 15,
        FeaturesJson = "{\"aiAssist\":true,\"fiveWhys\":true,\"aiSummary\":true}",
                IsActive = true,
   SortOrder = 3,
           CreatedAt = now,
  UpdatedAt = now
      }
        };

 dbContext.SubscriptionPlans.AddRange(plans);
  await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} subscription plans", plans.Count);
    }

    private static async Task SeedActivityTypeDefinitionsAsync(IPulseDbContext dbContext, ILogger logger)
  {
        var now = DateTimeOffset.UtcNow;

        // Check if activity types already exist
        var existingTypes = dbContext.ActivityTypeDefinitions.Any();
        if (existingTypes)
      {
  logger.LogDebug("Activity type definitions already seeded, skipping");
   return;
        }

        logger.LogInformation("Seeding activity type definitions...");

      var activityTypes = new List<ActivityTypeDefinitionRecord>
      {
    // Poll (Free)
            new()
 {
     Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        ActivityType = (int)ActivityType.Poll,
         DisplayName = "Poll",
     Description = "Multiple choice questions with single or multiple selection",
            IconClass = "ics ics-chart ic-sm",
     ColorHex = "#3B82F6",
           RequiresPremium = false,
      MinPlanCode = null,
    IsActive = true,
    SortOrder = 1,
                CreatedAt = now,
UpdatedAt = now
            },
            // WordCloud (Free)
      new()
      {
     Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
       ActivityType = (int)ActivityType.WordCloud,
              DisplayName = "Word Cloud",
                Description = "Collect words or short phrases from participants",
  IconClass = "ics ics-thought-balloon ic-sm",
ColorHex = "#10B981",
    RequiresPremium = false,
                MinPlanCode = null,
        IsActive = true,
SortOrder = 2,
       CreatedAt = now,
         UpdatedAt = now
            },
     // Quadrant (Free)
            new()
            {
Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
        ActivityType = (int)ActivityType.Quadrant,
        DisplayName = "Quadrant",
         Description = "Item scoring with bubble chart visualization",
        IconClass = "ics ics-chart-increasing ic-sm",
      ColorHex = "#8B5CF6",
     RequiresPremium = false,
        MinPlanCode = null,
        IsActive = true,
              SortOrder = 3,
      CreatedAt = now,
    UpdatedAt = now
        },
         // FiveWhys (Premium - Plan A+)
      new()
            {
    Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
           ActivityType = (int)ActivityType.FiveWhys,
    DisplayName = "Five Whys",
                Description = "AI-powered root cause analysis for problem-solving",
    IconClass = "ics ics-question ic-sm",
        ColorHex = "#F59E0B",
      RequiresPremium = true,
            MinPlanCode = "plan-a",
    IsActive = true,
         SortOrder = 4,
                CreatedAt = now,
       UpdatedAt = now
            },
            // Rating (Free)
        new()
       {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
  ActivityType = (int)ActivityType.Rating,
         DisplayName = "Rating",
      Description = "Star or numeric ratings with optional comments",
                IconClass = "ics ics-star ic-sm",
      ColorHex = "#EF4444",
             RequiresPremium = false,
 MinPlanCode = null,
    IsActive = true,
              SortOrder = 5,
          CreatedAt = now,
     UpdatedAt = now
      },
            // GeneralFeedback (Free)
         new()
            {
 Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            ActivityType = (int)ActivityType.GeneralFeedback,
 DisplayName = "Feedback",
    Description = "Open-ended feedback collection from participants",
       IconClass = "ics ics-chat ic-sm",
           ColorHex = "#06B6D4",
    RequiresPremium = false,
          MinPlanCode = null,
     IsActive = true,
           SortOrder = 6,
            CreatedAt = now,
      UpdatedAt = now
      },
    // QnA (Free)
            new()
   {
      Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
        ActivityType = (int)ActivityType.QnA,
       DisplayName = "Q&A",
   Description = "Live Q&A with upvoting and moderation",
    IconClass = "fas fa-lightbulb ic-sm",
           ColorHex = "#F97316",
     RequiresPremium = false,
    MinPlanCode = null,
        IsActive = true,
   SortOrder = 7,
           CreatedAt = now,
     UpdatedAt = now
    },
  // AiSummary (Premium - Plan A+)
     new()
    {
      Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
          ActivityType = (int)ActivityType.AiSummary,
      DisplayName = "AI Summary",
  Description = "AI-generated comprehensive session summary",
    IconClass = "fas fa-robot ic-sm",
         ColorHex = "#EC4899",
    RequiresPremium = true,
   MinPlanCode = "plan-a",
           IsActive = true,
     SortOrder = 8,
  CreatedAt = now,
  UpdatedAt = now
          },
         // Break (Free)
            new()
            {
        Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
              ActivityType = (int)ActivityType.Break,
     DisplayName = "Break",
 Description = "Timed break with countdown and ready signal",
       IconClass = "fas fa-coffee",
                ColorHex = "#6B7280",
     RequiresPremium = false,
          MinPlanCode = null,
                IsActive = true,
    SortOrder = 9,
    CreatedAt = now,
     UpdatedAt = now
          }
        };

        dbContext.ActivityTypeDefinitions.AddRange(activityTypes);
        await dbContext.SaveChangesAsync();

    logger.LogInformation("Seeded {Count} activity type definitions", activityTypes.Count);
    }
}
