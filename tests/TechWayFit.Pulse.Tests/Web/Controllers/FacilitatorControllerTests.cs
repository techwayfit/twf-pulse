using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Controllers;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Controllers;

public class FacilitatorControllerTests
{
    [Fact]
    public async Task Dashboard_Should_Redirect_To_Login_When_Not_Authenticated()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var sessionService = new Mock<ISessionService>();
        var groupService = new Mock<ISessionGroupService>();
        var authService = new Mock<IAuthenticationService>();

        var controller = new FacilitatorController(
            sessionRepository.Object,
            sessionService.Object,
            groupService.Object,
            authService.Object,
            NullLogger<FacilitatorController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = await controller.Dashboard();

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }
}
