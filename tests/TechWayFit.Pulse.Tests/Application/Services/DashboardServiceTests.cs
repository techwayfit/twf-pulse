using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class DashboardServiceTests
{
    private readonly Mock<IResponseRepository> _responses = new();
    private readonly Mock<IParticipantRepository> _participants = new();
    private readonly Mock<IActivityRepository> _activities = new();

    [Fact]
    public async Task GetDashboardAsync_Should_Return_QuadrantPoints_For_Quadrant_Activity()
    {
        var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _responses
            .Setup(x => x.GetByActivityAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Response>
            {
                CreateResponse(sessionId, activityId, participantId, "{\"x\":80,\"y\":90,\"label\":\"Quick win\"}"),
                CreateResponse(sessionId, activityId, participantId, "{\"x\":30,\"y\":20,\"label\":\"Low value\"}")
            });

        _participants
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Participant> { CreateParticipant(sessionId, participantId) });

        _activities
            .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActivity(sessionId, activityId, ActivityType.Quadrant));

        var service = new DashboardService(_responses.Object, _participants.Object, _activities.Object);

        var result = await service.GetDashboardAsync(sessionId, activityId, new Dictionary<string, string?>());

        result.QuadrantPoints.Should().HaveCount(2);
        result.QuadrantPoints.Select(p => p.Label).Should().Contain(new[] { "Quick win", "Low value" });
    }

    [Fact]
    public async Task GetDashboardAsync_Should_Not_Return_QuadrantPoints_For_NonQuadrant_Activity()
    {
        var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _responses
            .Setup(x => x.GetByActivityAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Response>
            {
                CreateResponse(sessionId, activityId, participantId, "{\"x\":80,\"y\":90,\"label\":\"Should be ignored\"}")
            });

        _participants
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Participant> { CreateParticipant(sessionId, participantId) });

        _activities
            .Setup(x => x.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActivity(sessionId, activityId, ActivityType.Poll));

        var service = new DashboardService(_responses.Object, _participants.Object, _activities.Object);

        var result = await service.GetDashboardAsync(sessionId, activityId, new Dictionary<string, string?>());

        result.QuadrantPoints.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardAsync_Should_Apply_Dimension_Filters()
    {
        var sessionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var participantA = Guid.NewGuid();
        var participantB = Guid.NewGuid();

        _responses
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Response>
            {
                CreateResponse(sessionId, activityId, participantA, "{\"text\":\"hello\"}", new Dictionary<string, string?> { ["Team"] = "Engineering" }),
                CreateResponse(sessionId, activityId, participantB, "{\"text\":\"world\"}", new Dictionary<string, string?> { ["Team"] = "Sales" })
            });

        _participants
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Participant>
            {
                CreateParticipant(sessionId, participantA),
                CreateParticipant(sessionId, participantB)
            });

        var service = new DashboardService(_responses.Object, _participants.Object, _activities.Object);

        var result = await service.GetDashboardAsync(
            sessionId,
            null,
            new Dictionary<string, string?> { ["Team"] = "Engineering" });

        result.TotalResponses.Should().Be(1);
        result.RespondedParticipants.Should().Be(1);
    }

    private static Activity CreateActivity(Guid sessionId, Guid activityId, ActivityType type)
    {
        return new Activity(
            activityId,
            sessionId,
            1,
            type,
            "Activity",
            "Prompt",
            "{}",
            ActivityStatus.Open,
            DateTimeOffset.UtcNow,
            null,
            5);
    }

    private static Participant CreateParticipant(Guid sessionId, Guid participantId)
    {
        return new Participant(
            participantId,
            sessionId,
            "Participant",
            false,
            new Dictionary<string, string?>(),
            DateTimeOffset.UtcNow);
    }

    private static Response CreateResponse(
        Guid sessionId,
        Guid activityId,
        Guid participantId,
        string payload,
        IReadOnlyDictionary<string, string?>? dimensions = null)
    {
        return new Response(
            Guid.NewGuid(),
            sessionId,
            activityId,
            participantId,
            payload,
            dimensions ?? new Dictionary<string, string?>(),
            DateTimeOffset.UtcNow);
    }
}
