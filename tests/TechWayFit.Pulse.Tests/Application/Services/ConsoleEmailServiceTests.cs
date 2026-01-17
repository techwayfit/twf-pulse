using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TechWayFit.Pulse.Application.Services;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class ConsoleEmailServiceTests
{
    [Fact]
    public async Task SendLoginOtpAsync_Should_Complete()
    {
        var service = new ConsoleEmailService(NullLogger<ConsoleEmailService>.Instance);

        var act = async () => await service.SendLoginOtpAsync("test@example.com", "123456", "Test User");

        await act.Should().NotThrowAsync();
    }
}
