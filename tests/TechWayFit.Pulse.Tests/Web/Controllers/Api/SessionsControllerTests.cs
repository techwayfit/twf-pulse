using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Controllers.Api;
using TechWayFit.Pulse.Web.Hubs;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Controllers.Api;

public class SessionsControllerTests
{
    [Fact]
    public async Task GetSession_Should_Return_NotFound_When_Missing()
    {
        var sessions = new Mock<ISessionService>();
        var authService = new Mock<IAuthenticationService>();
        var activities = new Mock<IActivityService>();
        var participants = new Mock<IParticipantService>();
        var responses = new Mock<IResponseService>();
        var dashboards = new Mock<IDashboardService>();
        var facilitatorTokens = new Mock<IFacilitatorTokenStore>();
        var codeGenerator = new Mock<ISessionCodeGenerator>();
        var hubContext = new Mock<IHubContext<WorkshopHub, IWorkshopClient>>();

        sessions
            .Setup(x => x.GetByCodeAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechWayFit.Pulse.Domain.Entities.Session?)null);

        var controller = new SessionsController(
            sessions.Object,
            authService.Object,
            activities.Object,
            participants.Object,
            responses.Object,
            dashboards.Object,
            facilitatorTokens.Object,
            codeGenerator.Object,
            hubContext.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetSession("MISSING", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
