using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Web.Controllers;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Controllers;

public class AccountControllerTests
{
    [Fact]
    public void Login_Should_Return_View_When_Not_Authenticated()
    {
        var authService = new Mock<IAuthenticationService>();
        var controller = new AccountController(
            authService.Object,
            NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = controller.Login("/return");

        result.Should().BeOfType<ViewResult>();
        controller.ViewData["ReturnUrl"].Should().Be("/return");
    }
}
