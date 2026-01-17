using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class ParticipantServiceTests
{
    [Fact]
    public async Task JoinAsync_Should_Throw_When_Unknown_Field()
    {
        var sessionId = Guid.NewGuid();
        var session = new Session(
            sessionId,
            "CODE",
            "Title",
            null,
            null,
            new SessionSettings(5, null, true, true, 60),
            new JoinFormSchema(1, new List<JoinFormField>
            {
                new("department", "Department", FieldType.Text, true, new List<string>(), true)
            }),
            SessionStatus.Live,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(60));

        var participants = new Mock<IParticipantRepository>();
        var sessions = new Mock<ISessionRepository>();

        sessions
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var service = new ParticipantService(participants.Object, sessions.Object);

        var act = async () => await service.JoinAsync(
            sessionId,
            "Name",
            false,
            new Dictionary<string, string?> { ["unknown"] = "value" },
            DateTimeOffset.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unknown join form field 'unknown'.");
    }
}
