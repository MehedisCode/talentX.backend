using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Application.Features.Notifications.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.Notifications;

public class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<INotificationBroadcaster> _notificationBroadcaster = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<NotificationService>> _logger = new();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _unitOfWork.SetupGet(unit => unit.Notifications).Returns(_notificationRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _notificationRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification notification, CancellationToken _) => notification);

        _service = new NotificationService(
            _unitOfWork.Object,
            _notificationBroadcaster.Object,
            _mapper.Object,
            _logger.Object);
    }

    [Fact]
    public async Task PublishAsync_ValidRequest_PersistsAndBroadcastsNotification()
    {
        Notification? capturedNotification = null;
        _notificationRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => capturedNotification = notification)
            .ReturnsAsync((Notification notification, CancellationToken _) => notification);

        NotificationDto? expectedDto = null;
        _mapper
            .Setup(mapper => mapper.Map<NotificationDto>(It.IsAny<Notification>()))
            .Returns((Notification notification) =>
            {
                expectedDto = new NotificationDto(
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.Metadata,
                    notification.ReadAt,
                    notification.CreatedAt);
                return expectedDto;
            });

        var result = await _service.PublishAsync(
            "talent-user-1",
            NotificationType.JobAlertMatch,
            "New match",
            "A job matches your alert",
            "{\"jobId\":\"abc\"}",
            CancellationToken.None);

        capturedNotification.Should().NotBeNull();
        capturedNotification!.RecipientUserId.Should().Be("talent-user-1");
        capturedNotification.Type.Should().Be(NotificationType.JobAlertMatch);
        result.Should().BeEquivalentTo(expectedDto);

        _notificationRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationBroadcaster.Verify(
            broadcaster => broadcaster.PublishToUserAsync("talent-user-1", It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenBroadcasterThrows_DoesNotPropagateAndStillReturnsDto()
    {
        _mapper
            .Setup(mapper => mapper.Map<NotificationDto>(It.IsAny<Notification>()))
            .Returns((Notification notification) => new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                notification.Metadata,
                notification.ReadAt,
                notification.CreatedAt));

        _notificationBroadcaster
            .Setup(broadcaster => broadcaster.PublishToUserAsync(
                It.IsAny<string>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broadcast failure"));

        var action = () => _service.PublishAsync(
            "talent-user-1",
            NotificationType.JobAlertMatch,
            "New match",
            "A job matches your alert",
            metadata: null,
            CancellationToken.None);

        var result = await action.Should().NotThrowAsync();

        result.Subject.Title.Should().Be("New match");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
