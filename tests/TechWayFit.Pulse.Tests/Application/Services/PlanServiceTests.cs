using Xunit;
using Moq;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace TechWayFit.Pulse.Tests.Application.Services;

public sealed class PlanServiceTests
{
    private readonly Mock<ISubscriptionPlanRepository> _mockPlanRepo;
    private readonly Mock<IFacilitatorSubscriptionRepository> _mockSubRepo;
    private readonly Mock<IActivityTypeDefinitionRepository> _mockActivityRepo;
    private readonly Mock<ILogger<PlanService>> _mockLogger;
    private readonly PlanService _sut;

    public PlanServiceTests()
    {
        _mockPlanRepo = new Mock<ISubscriptionPlanRepository>();
        _mockSubRepo = new Mock<IFacilitatorSubscriptionRepository>();
        _mockActivityRepo = new Mock<IActivityTypeDefinitionRepository>();
        _mockLogger = new Mock<ILogger<PlanService>>();

        _sut = new PlanService(
    _mockPlanRepo.Object,
    _mockSubRepo.Object,
     _mockActivityRepo.Object,
  _mockLogger.Object);
    }

    [Fact]
    public async Task CanCreateSessionAsync_WithNoSubscription_AutoAssignsFreeAndReturnsTrue()
    {
        // Arrange
var userId = Guid.NewGuid();
        var freePlanId = Guid.NewGuid();
        var freePlan = new TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan(
 freePlanId, "free", "Free Plan", "Free tier", 0m, null, 2, "{}", true, 0);

    _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
          .ReturnsAsync((FacilitatorSubscription?)null);

        _mockPlanRepo.Setup(x => x.GetByCodeAsync("free", default))
       .ReturnsAsync(freePlan);

        _mockSubRepo.Setup(x => x.AddAsync(It.IsAny<FacilitatorSubscription>(), default))
            .Returns(Task.CompletedTask);

        _mockPlanRepo.Setup(x => x.GetByIdAsync(freePlanId, default))
   .ReturnsAsync(freePlan);

        // Act
     var canCreate = await _sut.CanCreateSessionAsync(userId);

  // Assert
        Assert.True(canCreate);
        _mockSubRepo.Verify(x => x.AddAsync(
    It.Is<FacilitatorSubscription>(s => 
    s.FacilitatorUserId == userId && 
            s.PlanId == freePlanId &&
           s.SessionsUsed == 0), 
            default), Times.Once);
    }

    [Fact]
    public async Task CanCreateSessionAsync_WithQuotaExhausted_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
     var planId = Guid.NewGuid();
        var plan = new TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan(
         planId, "free", "Free Plan", "Free tier", 0m, null, 2, "{}", true, 0);

var subscription = new FacilitatorSubscription(
      Guid.NewGuid(),
            userId,
   planId,
   SubscriptionStatus.Active,
   DateTimeOffset.UtcNow.AddDays(-10),
       null,
       2, // Already used max sessions
       DateTimeOffset.UtcNow.AddMonths(1),
 null, null, null,
        DateTimeOffset.UtcNow.AddDays(-10),
    DateTimeOffset.UtcNow);

        _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
     .ReturnsAsync(subscription);

        _mockPlanRepo.Setup(x => x.GetByIdAsync(planId, default))
  .ReturnsAsync(plan);

        // Act
    var canCreate = await _sut.CanCreateSessionAsync(userId);

        // Assert
        Assert.False(canCreate);
    }

    [Fact]
    public async Task ConsumeSessionAsync_IncrementsSessionsUsed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plan = new TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan(
      planId, "free", "Free Plan", "Free tier", 0m, null, 2, "{}", true, 0);

        var subscription = new FacilitatorSubscription(
        Guid.NewGuid(),
         userId,
            planId,
      SubscriptionStatus.Active,
            DateTimeOffset.UtcNow.AddDays(-10),
         null,
       0, // No sessions used yet
   DateTimeOffset.UtcNow.AddMonths(1),
null, null, null,
            DateTimeOffset.UtcNow.AddDays(-10),
          DateTimeOffset.UtcNow);

        _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
       .ReturnsAsync(subscription);

        _mockPlanRepo.Setup(x => x.GetByCodeAsync("free", default))
            .ReturnsAsync(plan);

      _mockSubRepo.Setup(x => x.UpdateAsync(It.IsAny<FacilitatorSubscription>(), default))
         .Returns(Task.CompletedTask);

        // Act
      await _sut.ConsumeSessionAsync(userId);

     // Assert
      _mockSubRepo.Verify(x => x.UpdateAsync(
  It.Is<FacilitatorSubscription>(s => s.SessionsUsed == 1),
            default), Times.Once);
    }

    [Fact]
    public async Task CanUseFeatureAsync_WithFeatureEnabled_ReturnsTrue()
    {
        // Arrange
      var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var featuresJson = "{\"aiAssist\": true, \"fiveWhys\": true, \"aiSummary\": false}";
        var plan = new TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan(
            planId, "plan-a", "Plan A", "Premium", 29m, null, 10, featuresJson, true, 1);

     var subscription = new FacilitatorSubscription(
            Guid.NewGuid(),
     userId,
            planId,
 SubscriptionStatus.Active,
            DateTimeOffset.UtcNow.AddDays(-10),
      null,
            0,
 DateTimeOffset.UtcNow.AddMonths(1),
            null, null, null,
     DateTimeOffset.UtcNow.AddDays(-10),
   DateTimeOffset.UtcNow);

  _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
            .ReturnsAsync(subscription);

 _mockPlanRepo.Setup(x => x.GetByCodeAsync("free", default))
            .ReturnsAsync(plan);

        _mockPlanRepo.Setup(x => x.GetByIdAsync(planId, default))
    .ReturnsAsync(plan);

        // Act
        var canUse = await _sut.CanUseFeatureAsync(userId, "aiAssist");

      // Assert
      Assert.True(canUse);
    }

    [Fact]
    public async Task CanUseFeatureAsync_WithFeatureDisabled_ReturnsFalse()
    {
        // Arrange
    var userId = Guid.NewGuid();
   var planId = Guid.NewGuid();
        var featuresJson = "{\"aiAssist\": false, \"fiveWhys\": false, \"aiSummary\": false}";
      var plan = new TechWayFit.Pulse.Application.Abstractions.Repositories.SubscriptionPlan(
     planId, "free", "Free Plan", "Free tier", 0m, null, 2, featuresJson, true, 0);

        var subscription = new FacilitatorSubscription(
        Guid.NewGuid(),
         userId,
  planId,
  SubscriptionStatus.Active,
       DateTimeOffset.UtcNow.AddDays(-10),
       null,
         0,
   DateTimeOffset.UtcNow.AddMonths(1),
            null, null, null,
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow);

        _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
            .ReturnsAsync(subscription);

  _mockPlanRepo.Setup(x => x.GetByCodeAsync("free", default))
     .ReturnsAsync(plan);

        _mockPlanRepo.Setup(x => x.GetByIdAsync(planId, default))
     .ReturnsAsync(plan);

      // Act
  var canUse = await _sut.CanUseFeatureAsync(userId, "aiAssist");

    // Assert
      Assert.False(canUse);
    }

    [Fact]
    public async Task CanUseActivityTypeAsync_WithAvailableToAllPlans_ReturnsTrue()
    {
        // Arrange
 var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
      var subscription = new FacilitatorSubscription(
            Guid.NewGuid(),
       userId,
         planId,
            SubscriptionStatus.Active,
            DateTimeOffset.UtcNow.AddDays(-10),
            null,
            0,
            DateTimeOffset.UtcNow.AddMonths(1),
     null, null, null,
        DateTimeOffset.UtcNow.AddDays(-10),
       DateTimeOffset.UtcNow);

        var activityDef = new ActivityTypeDefinition(
  Guid.NewGuid(),
            ActivityType.Poll,
      "Poll",
"Simple poll",
         "fas fa-poll",
            "#FF0000",
   requiresPremium: false,
            applicablePlanIds: null,
    isAvailableToAllPlans: true,
   isActive: true,
   sortOrder: 1,
            DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);

        _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
            .ReturnsAsync(subscription);

        _mockActivityRepo.Setup(x => x.GetByActivityTypeAsync(ActivityType.Poll, default))
            .ReturnsAsync(activityDef);

        // Act
        var canUse = await _sut.CanUseActivityTypeAsync(userId, ActivityType.Poll);

        // Assert
        Assert.True(canUse);
    }

    [Fact]
    public async Task CanUseActivityTypeAsync_WithPremiumButNotInPlanList_ReturnsFalse()
    {
   // Arrange
      var userId = Guid.NewGuid();
 var freePlanId = Guid.NewGuid();
 var premiumPlanId = Guid.NewGuid();

        var subscription = new FacilitatorSubscription(
            Guid.NewGuid(),
            userId,
            freePlanId, // User is on Free plan
   SubscriptionStatus.Active,
    DateTimeOffset.UtcNow.AddDays(-10),
            null,
            0,
      DateTimeOffset.UtcNow.AddMonths(1),
null, null, null,
            DateTimeOffset.UtcNow.AddDays(-10),
 DateTimeOffset.UtcNow);

     var activityDef = new ActivityTypeDefinition(
  Guid.NewGuid(),
 ActivityType.FiveWhys,
 "5 Whys",
         "Root cause analysis",
     "fas fa-question",
    "#0000FF",
            requiresPremium: true,
      applicablePlanIds: premiumPlanId.ToString(), // Only premium plan has access
       isAvailableToAllPlans: false,
    isActive: true,
       sortOrder: 5,
            DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow);

        _mockSubRepo.Setup(x => x.GetActiveSubscriptionAsync(userId, default))
        .ReturnsAsync(subscription);

        _mockActivityRepo.Setup(x => x.GetByActivityTypeAsync(ActivityType.FiveWhys, default))
 .ReturnsAsync(activityDef);

        // Act
        var canUse = await _sut.CanUseActivityTypeAsync(userId, ActivityType.FiveWhys);

        // Assert
        Assert.False(canUse);
    }
}
