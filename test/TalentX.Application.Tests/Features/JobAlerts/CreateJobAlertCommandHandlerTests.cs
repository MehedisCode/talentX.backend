using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;
using TalentX.Application.Features.JobAlerts.Constants;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class CreateJobAlertCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly CreateJobAlertCommandHandler _handler;

    public CreateJobAlertCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateJobAlertCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndReturnsAlertId()
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Senior .NET Roles",
            Title: "Backend Engineer",
            Location: "Dhaka",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.SeniorLevel,
            WorkArrangement: WorkArrangement.FullRemote);

        var command = new CreateJobAlertCommand(parameters);

        _jobAlertRepository
            .Setup(repository => repository.CountByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        JobAlert? capturedAlert = null;
        _jobAlertRepository
            .Setup(repository => repository.AddAsync(It.IsAny<JobAlert>(), It.IsAny<CancellationToken>()))
            .Callback<JobAlert, CancellationToken>((entity, _) => capturedAlert = entity)
            .Returns(Task.CompletedTask);

        var resultId = await _handler.Handle(command, CancellationToken.None);

        resultId.Should().NotBeEmpty();
        capturedAlert.Should().NotBeNull();
        capturedAlert!.Name.Should().Be("Senior .NET Roles");
        capturedAlert.Title.Should().Be("Backend Engineer");
        capturedAlert.TalentId.Should().Be("talent-user-1");
        capturedAlert.IsEnabled.Should().BeTrue();

        _jobAlertRepository.Verify(repository => repository.AddAsync(It.IsAny<JobAlert>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMaxAlertsReached_ThrowsBadRequestException()
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Exceeding Alert",
            Title: "Architect");

        var command = new CreateJobAlertCommand(parameters);

        _jobAlertRepository
            .Setup(repository => repository.CountByTalentIdAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(JobAlertConstants.MaxAlertsPerTalent);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage($"*{JobAlertConstants.MaxAlertsPerTalent}*");

        _jobAlertRepository.Verify(repository => repository.AddAsync(It.IsAny<JobAlert>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new CreateJobAlertCommand(new CreateJobAlertParameters("Alert", "Developer"));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
