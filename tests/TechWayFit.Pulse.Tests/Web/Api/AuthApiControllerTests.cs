using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Api;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Api;

public class AuthApiControllerTests
{
    [Fact]
    public void CheckAuth_Should_Return_Ok_For_Anonymous()
    {
        var authService = new Mock<IAuthenticationService>();
        var controller = new AuthApiController(authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.CheckAuth();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            isAuthenticated = false,
            userName = (string?)null,
            userEmail = (string?)null
        });
    }
}
