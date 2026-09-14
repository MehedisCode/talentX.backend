using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.API.Controllers.Admin;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AuditLogs.DTOs;
using TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class AuditLogsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new AuditLogsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void Controller_InheritsBaseApiController()
    {
        typeof(AuditLogsController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void Controller_HasAuthorizeAttributeWithAdminRole()
    {
        var authorizeAttribute = typeof(AuditLogsController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void Controller_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(AuditLogsController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/admin/audit-logs");
    }

    [Fact]
    public void GetAuditLogs_HasExactlyOneHttpGetAttribute()
    {
        var method = typeof(AuditLogsController).GetMethod(nameof(AuditLogsController.GetAuditLogs));

        method.Should().NotBeNull();

        var httpMethodAttributes = method!.GetCustomAttributes().OfType<HttpMethodAttribute>().ToList();
        httpMethodAttributes.Should().ContainSingle();
        httpMethodAttributes[0].Should().BeOfType<HttpGetAttribute>();
    }

    [Fact]
    public async Task GetAuditLogs_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        // Arrange
        var query = new GetAuditLogsQuery(
            SearchQuery: "login",
            StartDate: DateTime.UtcNow.AddDays(-1),
            EndDate: DateTime.UtcNow,
            Page: 1,
            PageSize: 20);

        var expectedResult = new PaginatedList<AuditLogDto>(
            new List<AuditLogDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Timestamp = "2026-09-14 09:30:00",
                    Actor = "admin@talentx.com",
                    IsSystem = false,
                    Action = "Login Success"
                }
            },
            1,
            20,
            1);

        _sender
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAuditLogs(query, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogs_WithNullQuery_DispatchesDefaultQueryAndReturnsOk()
    {
        // Arrange
        var expectedResult = new PaginatedList<AuditLogDto>(new List<AuditLogDto>(), 1, 20, 0);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAuditLogs(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(
            sender => sender.Send(
                It.Is<GetAuditLogsQuery>(q => q.SearchQuery == null && q.Page == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
