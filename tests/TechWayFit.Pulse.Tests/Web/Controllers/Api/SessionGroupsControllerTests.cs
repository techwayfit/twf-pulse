using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Controllers.Api;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Controllers.Api;

public class SessionGroupsControllerTests
{
    [Fact]
    public async Task GetGroup_Should_Return_NotFound_When_Missing()
    {
        var sessionGroupService = new Mock<ISessionGroupService>();
        var sessionService = new Mock<ISessionService>();
        var sessionRepository = new Mock<ISessionRepository>();
        var authService = new Mock<IAuthenticationService>();

        sessionGroupService
            .Setup(x => x.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechWayFit.Pulse.Domain.Entities.SessionGroup?)null);

        var controller = new SessionGroupsController(
            sessionGroupService.Object,
            sessionService.Object,
            sessionRepository.Object,
            authService.Object,
            NullLogger<SessionGroupsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetGroup(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
