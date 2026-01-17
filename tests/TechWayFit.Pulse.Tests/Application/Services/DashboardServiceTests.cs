using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_Should_Filter_Responses_And_Build_WordCloud()
    {
        var sessionId = Guid.NewGuid();
        var participantA = Guid.NewGuid();
        var participantB = Guid.NewGuid();
        var activityId = Guid.NewGuid();

        var responses = new List<Response>
        {
            new(
                Guid.NewGuid(),
                sessionId,
                activityId,
                participantA,
                "\"hello world\"",
                new Dictionary<string, string?> { ["team"] = "A" },
                DateTimeOffset.UtcNow),
            new(
                Guid.NewGuid(),
                sessionId,
                activityId,
                participantB,
                "\"other\"",
                new Dictionary<string, string?> { ["team"] = "B" },
                DateTimeOffset.UtcNow)
        };

        var participants = new List<Participant>
        {
            new(participantA, sessionId, "A", false, new Dictionary<string, string?>(), DateTimeOffset.UtcNow),
            new(participantB, sessionId, "B", false, new Dictionary<string, string?>(), DateTimeOffset.UtcNow)
        };

        var responseRepo = new Mock<IResponseRepository>();
        var participantRepo = new Mock<IParticipantRepository>();
        var activityRepo = new Mock<IActivityRepository>();

        responseRepo
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        participantRepo
            .Setup(x => x.GetBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        var service = new DashboardService(
            responseRepo.Object,
            participantRepo.Object,
            activityRepo.Object);

        var dashboard = await service.GetDashboardAsync(
            sessionId,
            null,
            new Dictionary<string, string?> { ["team"] = "A" });

        dashboard.TotalResponses.Should().Be(1);
        dashboard.ParticipantCount.Should().Be(2);
        dashboard.RespondedParticipants.Should().Be(1);
        dashboard.WordCloud.Should().ContainSingle(item => item.Text == "hello" && item.Count == 1);
        dashboard.WordCloud.Should().ContainSingle(item => item.Text == "world" && item.Count == 1);
    }
}
