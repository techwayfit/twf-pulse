using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class ActivityServiceTests
{
    [Fact]
    public async Task AddActivityAsync_Should_Create_Activity()
    {
        var sessionId = Guid.NewGuid();
        var sessions = new Mock<ISessionRepository>();
        var activities = new Mock<IActivityRepository>();
        var session = CreateSession(sessionId);

        sessions
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        activities
            .Setup(x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ActivityService(activities.Object, sessions.Object);

        var activity = await service.AddActivityAsync(
            sessionId,
            1,
            ActivityType.WordCloud,
            "  Big Idea  ",
            "Prompt",
            null);

        activity.SessionId.Should().Be(sessionId);
        activity.Title.Should().Be("Big Idea");
        activity.Status.Should().Be(ActivityStatus.Pending);

        activities.Verify(
            x => x.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Session CreateSession(Guid sessionId)
    {
        return new Session(
            sessionId,
            "CODE",
            "Title",
            null,
            null,
            new SessionSettings(5, null, true, true, 60),
            new JoinFormSchema(5, new List<JoinFormField>()),
            SessionStatus.Draft,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(60));
    }
}
