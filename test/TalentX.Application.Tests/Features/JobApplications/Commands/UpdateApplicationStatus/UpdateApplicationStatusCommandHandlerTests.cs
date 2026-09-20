using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;
using TalentX.Application.Identity.Services;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.UpdateApplicationStatus;

public class UpdateApplicationStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateApplicationStatusCommandHandler _handler;

    public UpdateApplicationStatusCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");

        _handler = new UpdateApplicationStatusCommandHandler(
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    private static Job CreateJob(string employerId)
    {
        return Job.Create(
            employerId,
            "Product Designer",
            "Design Studio",
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "Figma" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static JobApplication CreateApplication(Guid jobId, string talentUserId)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Alex Smith",
            "alex@example.com",
            "resumes/alex.pdf");
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsJob_UpdatesStatusAndReturnsResponse()
    {
        var job = CreateJob("employer-1");
        var application = CreateApplication(job.Id, "talent-1");

        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateApplicationStatusCommand(application.Id, ApplicationStatus.Interview);

        var result = await _handler.Handle(command, CancellationToken.None);

        application.Status.Should().Be(ApplicationStatus.Interview);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<StatusUpdateResponseDto>();
        result.Message.Should().Be("Application status updated to Interview.");
    }

    [Fact]
    public async Task Handle_WhenCallerIsUnauthorized_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var command = new UpdateApplicationStatusCommand(Guid.NewGuid(), ApplicationStatus.Reviewed);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be authenticated to update an application status.");
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        var command = new UpdateApplicationStatusCommand(appId, ApplicationStatus.Hired);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job application '{appId}' was not found.");
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnJob_ThrowsForbiddenException()
    {
        var job = CreateJob("other-employer");
        var application = CreateApplication(job.Id, "talent-1");

        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateApplicationStatusCommand(application.Id, ApplicationStatus.Rejected);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to update status for this job application.");
    }
}
