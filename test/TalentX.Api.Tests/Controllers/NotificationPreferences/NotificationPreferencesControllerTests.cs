using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.NotificationPreferences;
using TalentX.API.Controllers.NotificationPreferences.Requests;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.NotificationPreferences.Commands.UpdateNotificationPreferences;
using TalentX.Application.Features.NotificationPreferences.Queries.GetNotificationPreferences;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.NotificationPreferences;

public class NotificationPreferencesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly NotificationPreferencesController _controller;

    public NotificationPreferencesControllerTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_sender.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _controller = new NotificationPreferencesController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_WithTalentRole()
    {
        var authorizeAttribute = typeof(NotificationPreferencesController)
            .GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Theory]
    [InlineData(nameof(NotificationPreferencesController.GetNotificationPreferences))]
    [InlineData(nameof(NotificationPreferencesController.UpdateNotificationPreferences))]
    public void Actions_HaveAuthorizeAttribute_WithTalentRole(string actionMethodName)
    {
        var methodInfo = typeof(NotificationPreferencesController).GetMethod(actionMethodName);

        methodInfo.Should().NotBeNull();
        var authorizeAttribute = methodInfo!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public async Task GetNotificationPreferences_Returns200OkWithPreferences()
    {
        var expectedPreferences = new TalentNotificationPreferenceDto(true);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetNotificationPreferencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPreferences);

        var actionResult = await _controller.GetNotificationPreferences(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedPreferences);

        _sender.Verify(
            sender => sender.Send(It.IsAny<GetNotificationPreferencesQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_ValidRequest_SendsCommandAndReturns204NoContent()
    {
        var updateRequest = new UpdateNotificationPreferencesRequest(false);

        _sender
            .Setup(sender => sender.Send(It.IsAny<UpdateNotificationPreferencesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var actionResult = await _controller.UpdateNotificationPreferences(updateRequest, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();

        _sender.Verify(
            sender => sender.Send(
                It.Is<UpdateNotificationPreferencesCommand>(command => !command.JobAlertsEmailEnabled),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
