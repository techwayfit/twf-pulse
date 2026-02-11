using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Services;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendLoginOtpAsync_Should_Throw_When_Smtp_Config_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailTemplates:BasePath"] = "/tmp"
            })
            .Build();

        var fileService = new Mock<IFileService>();
        fileService
            .Setup(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Template {{OTP_CODE}}");

        var service = new SmtpEmailService(
            config,
            fileService.Object,
            NullLogger<SmtpEmailService>.Instance);

        var act = async () => await service.SendLoginOtpAsync("test@example.com", "123456", "Test User");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMTP configuration is incomplete. Check Host, Username, and Password settings.");
    }
}
