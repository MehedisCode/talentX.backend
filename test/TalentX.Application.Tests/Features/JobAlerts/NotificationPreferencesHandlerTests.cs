using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.NotificationPreferences.Commands.UpdateNotificationPreferences;
using TalentX.Application.Features.NotificationPreferences.Queries.GetNotificationPreferences;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class NotificationPreferencesHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentNotificationPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly GetNotificationPreferencesQueryHandler _getQueryHandler;
    private readonly UpdateNotificationPreferencesCommandHandler _updateCommandHandler;

    public NotificationPreferencesHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.TalentNotificationPreferences).Returns(_preferenceRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _getQueryHandler = new GetNotificationPreferencesQueryHandler(_unitOfWork.Object, _currentUserService.Object);
        _updateCommandHandler = new UpdateNotificationPreferencesCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Get_WhenPreferencesExist_ReturnsCurrentPreferences()
    {
        var preference = TalentNotificationPreference.CreateDefault("talent-user-1");
        preference.Update(false);

        _preferenceRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var result = await _getQueryHandler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.JobAlertsEmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Get_WhenPreferencesDoNotExist_ReturnsDefaultEnabledTrue()
    {
        _preferenceRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);

        var result = await _getQueryHandler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.JobAlertsEmailEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Get_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var action = () => _getQueryHandler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Update_WhenPreferencesDoNotExist_CreatesDefaultAndUpdatesPreference()
    {
        _preferenceRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TalentNotificationPreference?)null);

        TalentNotificationPreference? capturedPreference = null;
        _preferenceRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TalentNotificationPreference>(), It.IsAny<CancellationToken>()))
            .Callback<TalentNotificationPreference, CancellationToken>((entity, _) => capturedPreference = entity)
            .Returns(Task.CompletedTask);

        var command = new UpdateNotificationPreferencesCommand(JobAlertsEmailEnabled: false);

        await _updateCommandHandler.Handle(command, CancellationToken.None);

        capturedPreference.Should().NotBeNull();
        capturedPreference!.TalentId.Should().Be("talent-user-1");
        capturedPreference.JobAlertsEmailEnabled.Should().BeFalse();

        _preferenceRepository.Verify(repository => repository.AddAsync(It.IsAny<TalentNotificationPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenPreferencesExist_UpdatesExistingPreference()
    {
        var existingPreference = TalentNotificationPreference.CreateDefault("talent-user-1");
        existingPreference.JobAlertsEmailEnabled.Should().BeTrue();

        _preferenceRepository
            .Setup(repository => repository.GetByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        var command = new UpdateNotificationPreferencesCommand(JobAlertsEmailEnabled: false);

        await _updateCommandHandler.Handle(command, CancellationToken.None);

        existingPreference.JobAlertsEmailEnabled.Should().BeFalse();
        existingPreference.UpdatedAt.Should().NotBeNull();

        _preferenceRepository.Verify(repository => repository.AddAsync(It.IsAny<TalentNotificationPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new UpdateNotificationPreferencesCommand(JobAlertsEmailEnabled: true);

        var action = () => _updateCommandHandler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
