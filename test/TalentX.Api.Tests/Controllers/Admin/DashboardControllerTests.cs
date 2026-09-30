using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.API.Controllers.Admin;
using TalentX.Application.Features.AdminDashboard.DTOs;
using TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class DashboardControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new DashboardController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void DashboardController_Class_InheritsBaseApiController()
    {
        typeof(DashboardController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void DashboardController_Class_RequiresAdminRole()
    {
        var authorizeAttribute = typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void DashboardController_Class_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(DashboardController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/admin/dashboard");
    }

    [Fact]
    public void GetDashboard_Action_IsAnHttpGetWithoutItsOwnRouteSegment()
    {
        var method = typeof(DashboardController).GetMethod(nameof(DashboardController.GetDashboard))!;

        var httpGet = method.GetCustomAttribute<HttpGetAttribute>();

        httpGet.Should().NotBeNull();
        httpGet!.Template.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboard_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        var query = new GetAdminDashboardQuery
        {
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc)
        };
        var expected = new AdminDashboardDto { Metrics = new AdminDashboardMetricsDto { TotalTalents = 5 } };
        _sender
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetDashboard(query, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
        _sender.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDashboard_WithNullQuery_DispatchesDefaultQueryAndReturnsOk()
    {
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAdminDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminDashboardDto());

        var result = await _controller.GetDashboard(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<GetAdminDashboardQuery>(q => q.StartDate == null && q.EndDate == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
