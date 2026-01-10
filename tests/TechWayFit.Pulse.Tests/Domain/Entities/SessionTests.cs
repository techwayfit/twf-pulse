using FluentAssertions;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Domain.Entities;

public class SessionTests
{
    [Fact]
    public void Session_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = "TEST-2024";
        var title = "Test Session";
        var goal = "Test Goal";
        var context = "Test Context";
        var settings = new SessionSettings(5, null, true, true, 360);
        var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());
        var status = SessionStatus.Draft;
        var createdAt = DateTimeOffset.UtcNow;
     var updatedAt = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(360);

        // Act
        var session = new Session(
            id, code, title, goal, context, settings, joinFormSchema, 
            status, null, createdAt, updatedAt, expiresAt);

        // Assert
        session.Id.Should().Be(id);
      session.Code.Should().Be(code);
        session.Title.Should().Be(title);
        session.Goal.Should().Be(goal);
        session.Context.Should().Be(context);
      session.Settings.Should().Be(settings);
        session.JoinFormSchema.Should().Be(joinFormSchema);
        session.Status.Should().Be(status);
session.CurrentActivityId.Should().BeNull();
  session.CreatedAt.Should().Be(createdAt);
        session.UpdatedAt.Should().Be(updatedAt);
        session.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void SetStatus_Should_Update_Status_And_UpdatedAt()
    {
        // Arrange
      var session = CreateTestSession();
        var newStatus = SessionStatus.Live;
        var newUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        // Act
        session.SetStatus(newStatus, newUpdatedAt);

        // Assert
        session.Status.Should().Be(newStatus);
    session.UpdatedAt.Should().Be(newUpdatedAt);
    }

  [Fact]
    public void SetCurrentActivity_Should_Update_CurrentActivityId_And_UpdatedAt()
    {
        // Arrange
      var session = CreateTestSession();
   var activityId = Guid.NewGuid();
     var newUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

      // Act
        session.SetCurrentActivity(activityId, newUpdatedAt);

        // Assert
        session.CurrentActivityId.Should().Be(activityId);
    session.UpdatedAt.Should().Be(newUpdatedAt);
    }

    private static Session CreateTestSession()
    {
        return new Session(
        Guid.NewGuid(),
   "TEST-2024",
  "Test Session",
    "Test Goal",
   "Test Context",
    new SessionSettings(5, null, true, true, 360),
     new JoinFormSchema(5, new List<JoinFormField>()),
      SessionStatus.Draft,
  null,
    DateTimeOffset.UtcNow,
  DateTimeOffset.UtcNow,
     DateTimeOffset.UtcNow.AddMinutes(360));
    }
}