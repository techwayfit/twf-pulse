using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class ActivityServiceTests
{
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly ActivityService _activityService;

    public ActivityServiceTests()
    {
        _activityRepositoryMock = new Mock<IActivityRepository>();
 _sessionRepositoryMock = new Mock<ISessionRepository>();
        _activityService = new ActivityService(_activityRepositoryMock.Object, _sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task CopyActivityAsync_Should_Create_Copy_Successfully()
    {
   // Arrange
  var sessionId = Guid.NewGuid();
var activityId = Guid.NewGuid();
        var sourceActivity = CreateTestActivity(sessionId, activityId, "Original Activity", 1);

  var existingActivities = new List<Activity> { sourceActivity };

        _activityRepositoryMock
            .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(sourceActivity);

        _activityRepositoryMock
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingActivities);

        _activityRepositoryMock
     .Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

        // Act
   var result = await _activityService.CopyActivityAsync(sessionId, activityId);

  // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(activityId); // Different ID
      result.SessionId.Should().Be(sessionId);
        result.Title.Should().Be("Original Activity (Copy)");
        result.Prompt.Should().Be(sourceActivity.Prompt);
   result.Config.Should().Be(sourceActivity.Config);
   result.Type.Should().Be(sourceActivity.Type);
        result.Status.Should().Be(ActivityStatus.Pending);
    result.Order.Should().Be(2); // Appended after existing activity
        result.DurationMinutes.Should().Be(sourceActivity.DurationMinutes);

        _activityRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Activity>(a =>
          a.Title == "Original Activity (Copy)" &&
   a.SessionId == sessionId &&
 a.Status == ActivityStatus.Pending &&
        a.Order == 2
        ), It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Fact]
    public async Task CopyActivityAsync_Should_Truncate_Title_If_Too_Long()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
      var activityId = Guid.NewGuid();
     var longTitle = new string('A', 195); // 195 + " (Copy)" = 202, exceeds 200 limit
        var sourceActivity = CreateTestActivity(sessionId, activityId, longTitle, 1);

        _activityRepositoryMock
    .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
     .ReturnsAsync(sourceActivity);

        _activityRepositoryMock
          .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Activity> { sourceActivity });

   _activityRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

        // Act
        var result = await _activityService.CopyActivityAsync(sessionId, activityId);

     // Assert
     result.Title.Should().EndWith(" (Copy)");
        result.Title.Length.Should().BeLessOrEqualTo(200);
    }

    [Fact]
    public async Task CopyActivityAsync_Should_Throw_When_Activity_Not_Found()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();

      _activityRepositoryMock
         .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
       .ReturnsAsync((Activity?)null);

  // Act & Assert
        var act = async () => await _activityService.CopyActivityAsync(sessionId, activityId);

     await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("Activity not found.");
    }

    [Fact]
public async Task CopyActivityAsync_Should_Throw_When_Activity_Belongs_To_Different_Session()
    {
     // Arrange
        var sessionId = Guid.NewGuid();
        var differentSessionId = Guid.NewGuid();
  var activityId = Guid.NewGuid();
        var sourceActivity = CreateTestActivity(differentSessionId, activityId, "Activity", 1);

   _activityRepositoryMock
            .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceActivity);

        // Act & Assert
      var act = async () => await _activityService.CopyActivityAsync(sessionId, activityId);

        await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("Activity does not belong to the session.");
    }

    [Fact]
    public async Task CopyActivityAsync_Should_Preserve_All_Properties_Except_Status()
    {
   // Arrange
    var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var config = "{\"options\":[\"A\",\"B\",\"C\"]}";
        var sourceActivity = new Activity(
  activityId,
   sessionId,
            1,
ActivityType.Poll,
            "Test Poll",
            "What do you prefer?",
            config,
            ActivityStatus.Closed, // Source is closed
     DateTimeOffset.UtcNow.AddMinutes(-30),
  DateTimeOffset.UtcNow.AddMinutes(-20),
   10);

        _activityRepositoryMock
        .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(sourceActivity);

        _activityRepositoryMock
          .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
     .ReturnsAsync(new List<Activity> { sourceActivity });

   _activityRepositoryMock
       .Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

      // Act
        var result = await _activityService.CopyActivityAsync(sessionId, activityId);

        // Assert
      result.Type.Should().Be(ActivityType.Poll);
     result.Prompt.Should().Be("What do you prefer?");
        result.Config.Should().Be(config);
  result.DurationMinutes.Should().Be(10);
        result.Status.Should().Be(ActivityStatus.Pending); // Always Pending for copies
        result.OpenedAt.Should().BeNull(); // Not opened
        result.ClosedAt.Should().BeNull(); // Not closed
    }

    private static Activity CreateTestActivity(
        Guid sessionId,
      Guid activityId,
string title,
        int order,
     ActivityType type = ActivityType.Poll)
    {
        return new Activity(
        activityId,
   sessionId,
  order,
     type,
       title,
            "Test prompt",
      "{\"test\":\"config\"}",
ActivityStatus.Pending,
            null,
      null,
    5);
    }
}
