using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Services;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task SendLoginOtpAsync_Should_Fail_When_Email_Missing()
    {
        var users = new Mock<IFacilitatorUserRepository>();
        var otps = new Mock<ILoginOtpRepository>();
        var emailService = new Mock<IEmailService>();

        var service = new AuthenticationService(
            users.Object,
            otps.Object,
            emailService.Object,
            NullLogger<AuthenticationService>.Instance);

        var result = await service.SendLoginOtpAsync(" ");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email is required.");
    }
}
