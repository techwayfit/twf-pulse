using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class SessionCodeGeneratorTests
{
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly Mock<ISessionRepository> _sessions;
    private readonly SessionCodeGenerator _generator;

    public SessionCodeGeneratorTests()
    {
        _sessions = new Mock<ISessionRepository>();
        _generator = new SessionCodeGenerator(_sessions.Object);
    }

    [Fact]
    public async Task GenerateUniqueCodeAsync_Should_Return_Formatted_Code()
    {
        _sessions
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        var code = await _generator.GenerateUniqueCodeAsync();

        code.Should().MatchRegex("^[A-Z2-9]{3}-[A-Z2-9]{3}-[A-Z2-9]{3}$");
        code.Replace("-", string.Empty)
            .All(character => AllowedChars.Contains(character))
            .Should().BeTrue();
    }

    [Fact]
    public async Task GenerateUniqueCodeAsync_Should_Retry_Until_Unique()
    {
        var existingSession = CreateSession("EXIST-001");

        _sessions
            .SetupSequence(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession)
            .ReturnsAsync((Session?)null);

        var code = await _generator.GenerateUniqueCodeAsync();

        code.Should().NotBeNullOrWhiteSpace();
        _sessions.Verify(
            x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateUniqueCodeAsync_Should_Throw_When_Max_Retries_Exceeded()
    {
        var existingSession = CreateSession("EXIST-002");

        _sessions
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        var act = async () => await _generator.GenerateUniqueCodeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to generate a unique session code after 10 attempts.");
    }

    private static Session CreateSession(string code)
    {
        return new Session(
            Guid.NewGuid(),
            code,
            "Session",
            "Goal",
            "Context",
            new SessionSettings(5, null, true, true, 360),
            new JoinFormSchema(5, new List<JoinFormField>()),
            SessionStatus.Draft,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(360));
    }
}
