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
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;
using TalentX.Application.Features.Users.DTOs;
using TalentX.Application.Features.Users.Queries.GetAdminUserStats;
using TalentX.Application.Features.Users.Queries.GetUserDetails;
using TalentX.Application.Features.Users.Queries.GetUsers;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class UsersControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new UsersController
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
        typeof(UsersController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Theory]
    [InlineData(nameof(UsersController.GetUsers))]
    [InlineData(nameof(UsersController.GetUserStats))]
    [InlineData(nameof(UsersController.GetUserDetails))]
    [InlineData(nameof(UsersController.UpdateUserAccountStatus))]
    public void Action_HasAuthorizeAttributeWithAdminRole(string actionName)
    {
        var authorizeAttribute = typeof(UsersController)
            .GetMethod(actionName)!
            .GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void Controller_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(UsersController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/admin/users");
    }

    [Fact]
    public async Task GetUsers_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        // Arrange
        var query = new GetUsersQuery { SearchQuery = "jordan", Role = UserRole.Talent, Page = 1, PageSize = 20 };
        var expectedResult = new PaginatedList<AdminUserListItemDto>(
            new List<AdminUserListItemDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Jordan Smith", Email = "jordan@talentx.com", Role = UserRole.Talent }
            },
            1,
            20,
            1);

        _sender
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUsers(query, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUsers_WithNullQuery_DispatchesDefaultQueryAndReturnsOk()
    {
        // Arrange
        var expectedResult = new PaginatedList<AdminUserListItemDto>(new List<AdminUserListItemDto>(), 1, 20, 0);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUsers(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(
            sender => sender.Send(
                It.Is<GetUsersQuery>(q => q.SearchQuery == null && q.Page == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserStats_DispatchesToSenderAndReturnsOk()
    {
        // Arrange
        var expectedResult = new AdminUserStatsDto { TotalUsers = 14, TalentsCount = 10, EmployersCount = 4 };

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAdminUserStatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUserStats(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(sender => sender.Send(It.IsAny<GetAdminUserStatsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserDetails_WithId_DispatchesQueryWithMatchingUserIdAndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedResult = new AdminUserDetailsDto { Id = userId, Name = "Jordan Smith", Role = UserRole.Talent };

        _sender
            .Setup(sender => sender.Send(It.Is<GetUserDetailsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUserDetails(userId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(
            sender => sender.Send(It.Is<GetUserDetailsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAccountStatus_WithCommand_RebindsRouteIdAndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserAccountStatusCommand(Guid.Empty, AccountStatus.Deactivated);
        var expectedResult = new StatusUpdateResponseDto("Account status updated to Deactivated.");

        _sender
            .Setup(sender => sender.Send(It.Is<UpdateUserAccountStatusCommand>(c => c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UpdateUserAccountStatus(userId, command, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(
            sender => sender.Send(It.Is<UpdateUserAccountStatusCommand>(c => c.UserId == userId && c.NewStatus == AccountStatus.Deactivated), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
