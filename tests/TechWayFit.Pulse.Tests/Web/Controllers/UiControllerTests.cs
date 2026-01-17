using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TechWayFit.Pulse.Web.Controllers;
using Xunit;

namespace TechWayFit.Pulse.Tests.Web.Controllers;

public class UiControllerTests
{
    [Fact]
    public void Index_Should_Return_View()
    {
        var controller = new UiController();

        var result = controller.Index();

        result.Should().BeOfType<ViewResult>();
    }
}
