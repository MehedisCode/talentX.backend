using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Notifications;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using TalentX.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Application.Features.Notifications.Queries.GetNotifications;
using TalentX.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Notifications;

public class NotificationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new NotificationsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public async Task GetNotifications_SendsGetNotificationsQuery_AndReturnsOkResult()
    {
        var expectedResult = new PaginatedList<NotificationDto>(
            new List<NotificationDto>
            {
                new(Guid.NewGuid(), NotificationType.JobAlertMatch, "New match", "Body", null, null, DateTime.UtcNow)
            },
            1,
            20,
            1);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var actionResult = await _controller.GetNotifications(1, 20, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(
            sender => sender.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_SendsGetUnreadNotificationCountQuery_AndReturnsOkResult()
    {
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetUnreadNotificationCountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var actionResult = await _controller.GetUnreadCount(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(3);

        _sender.Verify(
            sender => sender.Send(It.IsAny<GetUnreadNotificationCountQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_SendsMarkNotificationAsReadCommand_AndReturnsNoContent()
    {
        var notificationId = Guid.NewGuid();

        _sender
            .Setup(sender => sender.Send(It.IsAny<MarkNotificationAsReadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.MarkAsRead(notificationId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();

        _sender.Verify(
            sender => sender.Send(
                It.Is<MarkNotificationAsReadCommand>(command => command.NotificationId == notificationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_SendsMarkAllNotificationsAsReadCommand_AndReturnsNoContent()
    {
        _sender
            .Setup(sender => sender.Send(It.IsAny<MarkAllNotificationsAsReadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.MarkAllAsRead(CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();

        _sender.Verify(
            sender => sender.Send(It.IsAny<MarkAllNotificationsAsReadCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
