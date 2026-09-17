using AutoMapper;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Application.Features.Notifications.Queries.GetNotifications;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.Notifications;

public class GetNotificationsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetNotificationsQueryHandler _handler;

    public GetNotificationsQueryHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.Notifications).Returns(_notificationRepository.Object);

        _handler = new GetNotificationsQueryHandler(
            _unitOfWork.Object,
            _currentUserService.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task Handle_WhenNotificationsExist_ReturnsMappedPaginatedList()
    {
        var notification = Notification.Create(
            "talent-user-1",
            NotificationType.JobAlertMatch,
            "New match",
            "A job matches your alert",
            "{\"jobId\":\"abc\"}");

        var notifications = new List<Notification> { notification };
        var paginatedNotifications = new PaginatedList<Notification>(notifications, 1, 20, 1);

        var expectedDtos = new List<NotificationDto>
        {
            new(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                notification.Metadata,
                notification.ReadAt,
                notification.CreatedAt)
        };

        _notificationRepository
            .Setup(repository => repository.GetByRecipientAsync("talent-user-1", 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedNotifications);

        _mapper
            .Setup(mapper => mapper.Map<List<NotificationDto>>(notifications))
            .Returns(expectedDtos);

        var result = await _handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("New match");
        result.TotalCount.Should().Be(1);

        _notificationRepository.Verify(
            repository => repository.GetByRecipientAsync("talent-user-1", 1, 20, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoNotifications_ReturnsEmptyPaginatedList()
    {
        var paginatedNotifications = new PaginatedList<Notification>(new List<Notification>(), 1, 20, 0);

        _notificationRepository
            .Setup(repository => repository.GetByRecipientAsync("talent-user-1", 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedNotifications);

        _mapper
            .Setup(mapper => mapper.Map<List<NotificationDto>>(It.IsAny<List<Notification>>()))
            .Returns(new List<NotificationDto>());

        var result = await _handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUnreadOnlyIsTrue_ForwardsUnreadOnlyToRepository()
    {
        var paginatedNotifications = new PaginatedList<Notification>(new List<Notification>(), 1, 20, 0);

        _notificationRepository
            .Setup(repository => repository.GetByRecipientAsync("talent-user-1", 1, 20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedNotifications);

        _mapper
            .Setup(mapper => mapper.Map<List<NotificationDto>>(It.IsAny<List<Notification>>()))
            .Returns(new List<NotificationDto>());

        var query = new GetNotificationsQuery(Page: 1, PageSize: 20, UnreadOnly: true);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);

        _notificationRepository.Verify(
            repository => repository.GetByRecipientAsync("talent-user-1", 1, 20, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var action = () => _handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
