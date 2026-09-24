using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Commands.PublishJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.PublishJob;

public sealed class PublishJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IEmployerRepository> _employerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IJobAlertQueue> _jobAlertQueue = new();
    private readonly PublishJobCommandHandler _handler;

    public PublishJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-123");
        _handler = new PublishJobCommandHandler(
            _jobRepository.Object,
            _employerRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object,
            _jobAlertQueue.Object);
    }

    private static Job CreateSampleJob(string employerId = "employer-123")
    {
        return Job.Create(
            employerId,
            "Senior Backend Engineer",
            "Tech Innovators Inc",
            "Develop robust distributed systems.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#", ".NET", "PostgreSQL"],
            2,
            DateTime.UtcNow.AddDays(30));
    }

    private static Employer CreateVerifiedEmployer(string userId = "employer-123")
    {
        var employer = new Employer(userId, "John Doe", "john@example.com", "Tech Innovators Inc");
        employer.MarkAsVerified();
        return employer;
    }

    private static Employer CreateUnverifiedEmployer(string userId = "employer-123")
    {
        return new Employer(userId, "John Doe", "john@example.com", "Tech Innovators Inc");
    }

    [Fact]
    public async Task Handle_ValidDraftJob_PublishesAndEnqueuesAlert()
    {
        var targetJob = CreateSampleJob(employerId: "employer-123");
        // Create sets it to Draft by default, let's make sure it has required fields filled for JobRules
        targetJob.UpdateWizardDetails(
            city: targetJob.City,
            country: targetJob.Country,
            minOverlapHours: 4,
            responsibilities: "Responsibilities",
            requirements: "Requirements",
            salaryType: SalaryType.Fixed
        );

        var employer = CreateVerifiedEmployer();

        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);
        _employerRepository.Setup(repository => repository.GetByUserIdAsync("employer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var command = new PublishJobCommand(targetJob.Id);
        var commandResult = await _handler.Handle(command, CancellationToken.None);

        commandResult.Should().Be(MediatR.Unit.Value);
        targetJob.Status.Should().Be(JobStatus.Published);
        targetJob.PublishedAt.Should().NotBeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(targetJob.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        var targetJobId = Guid.NewGuid();
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new PublishJobCommand(targetJobId);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job with ID '{targetJobId}' was not found.");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JobOwnedByOtherEmployer_ThrowsForbiddenException()
    {
        var targetJob = CreateSampleJob(employerId: "other-employer-999");
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);

        var command = new PublishJobCommand(targetJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to publish this job.");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnverifiedEmployer_ThrowsForbiddenException()
    {
        var targetJob = CreateSampleJob(employerId: "employer-123");
        var employer = CreateUnverifiedEmployer();

        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);
        _employerRepository.Setup(repository => repository.GetByUserIdAsync("employer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var command = new PublishJobCommand(targetJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only verified employers can publish jobs. Your account is pending verification.");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_IncompleteDraftJob_ThrowsBadRequestException()
    {
        // Job missing required fields (empty title)
        var targetJob = Job.Create(
            "employer-123",
            "",
            "Tech Innovators Inc",
            "Develop robust distributed systems.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#", ".NET", "PostgreSQL"],
            2,
            DateTime.UtcNow.AddDays(30));

        var employer = CreateVerifiedEmployer();

        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);
        _employerRepository.Setup(repository => repository.GetByUserIdAsync("employer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var command = new PublishJobCommand(targetJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Job cannot be published. Invalid or missing fields:*");
        
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
