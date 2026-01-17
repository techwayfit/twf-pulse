using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class ResponseServiceTests
{
    [Fact]
    public async Task SubmitAsync_Should_Throw_When_Session_Not_Live()
    {
        var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var sessions = new Mock<ISessionRepository>();
        var activities = new Mock<IActivityRepository>();
        var participants = new Mock<IParticipantRepository>();
        var responses = new Mock<IResponseRepository>();
        var counters = new Mock<IContributionCounterRepository>();

        sessions
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(sessionId, SessionStatus.Draft));

        var service = new ResponseService(
            responses.Object,
            sessions.Object,
            activities.Object,
            participants.Object,
            counters.Object);

        var act = async () => await service.SubmitAsync(
            sessionId,
            activityId,
            participantId,
            "payload",
            DateTimeOffset.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Session is not live.");
    }

    private static Session CreateSession(Guid sessionId, SessionStatus status)
    {
        return new Session(
            sessionId,
            "CODE",
            "Title",
            null,
            null,
            new SessionSettings(5, null, true, true, 60),
            new JoinFormSchema(5, new List<JoinFormField>()),
            status,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(60));
    }
}
