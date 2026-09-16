using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IEmployerRepository> _employerRepository = new();
    private readonly Mock<IJobAlertQueue> _jobAlertQueue = new();
    private readonly Mock<ILogger<UpdateJobCommandHandler>> _logger = new();
    private readonly UpdateJobCommandHandler _handler;

    public UpdateJobCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Employers).Returns(_employerRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _handler = new UpdateJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object,
            _jobAlertQueue.Object,
            _logger.Object);
    }

    private Employer SetupVerifiedEmployer(string userId = "employer-1")
    {
        var employer = new Employer(userId, "Test Employer", "employer@test.com", "Test Corp");
        employer.MarkEmailVerified();
        employer.MarkAsVerified();
        _employerRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);
        return employer;
    }

    private static Job ExistingDraftJob()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );

        // Set wizard fields for AnywhereFullRemote. SalaryType.Negotiable and
        // Requirements make this job already publish-ready without the update
        // itself resending them.
        job.UpdateWizardDetails(
            null,
            null,
            4,
            null,
            SalaryType.Negotiable,
            requirements: "- 3-5 years of relevant experience\n- Bachelor's degree or equivalent"
        );

        return job;
    }

    private static UpdateJobCommand CommandFor(Guid id, bool publish = false) => new(
        id, "New Title", null, null, null, null, null, null, null, null, null, publish
    );

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(CommandFor(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_DoesNotSave()
    {
        var id = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        try
        {
            await _handler.Handle(CommandFor(id), CancellationToken.None);
        }
        catch (NotFoundException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwningEmployer_ThrowsForbiddenException()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserService.SetupGet(u => u.UserId).Returns("some-other-employer");

        var act = () => _handler.Handle(CommandFor(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwningEmployer_DoesNotSave()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserService.SetupGet(u => u.UserId).Returns("some-other-employer");

        try
        {
            await _handler.Handle(CommandFor(job.Id), CancellationToken.None);
        }
        catch (ForbiddenException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobExists_AppliesUpdatesAndSaves()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(CommandFor(job.Id), CancellationToken.None);

        job.Title.Should().Be("New Title");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndCompleteData_PublishesTheJob()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        SetupVerifiedEmployer();

        await _handler.Handle(CommandFor(job.Id, publish: true), CancellationToken.None);

        job.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndIncompleteData_ThrowsValidationException()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        SetupVerifiedEmployer();

        // Force the existing job into an unpublishable state via the update itself.
        var invalidCommand = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, 0, null, true
        );

        var act = () => _handler.Handle(invalidCommand, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndIncompleteData_DoesNotSave()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        SetupVerifiedEmployer();

        var invalidCommand = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, 0, null, true
        );

        try
        {
            await _handler.Handle(invalidCommand, CancellationToken.None);
        }
        catch (FluentValidation.ValidationException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_DoesNotSave()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        try
        {
            await _handler.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None);
        }
        catch (UnauthorizedException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCityUpdate_UpdatesCityOnJob()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, null, null, false,
            City: "Chattogram", Country: null, MinOverlapHours: null);

        await _handler.Handle(command, CancellationToken.None);

        job.City.Should().Be("Chattogram");
    }

    [Fact]
    public async Task Handle_WithCountryUpdate_UpdatesCountryOnJob()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, null, null, false,
            City: null, Country: "India", MinOverlapHours: null);

        await _handler.Handle(command, CancellationToken.None);

        job.Country.Should().Be("India");
    }

    [Fact]
    public async Task Handle_WithMinOverlapHoursUpdate_UpdatesMinOverlapHoursOnJob()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, null, null, false,
            City: null, Country: null, MinOverlapHours: 8);

        await _handler.Handle(command, CancellationToken.None);

        job.MinOverlapHours.Should().Be(8);
    }

    [Fact]
    public async Task Handle_WithWizardFieldsUpdate_NormalizesCityAndCountry()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, null, null, false,
            City: "  Dhaka  ", Country: "  Bangladesh  ", MinOverlapHours: null);

        await _handler.Handle(command, CancellationToken.None);

        job.City.Should().Be("Dhaka");
        job.Country.Should().Be("Bangladesh");
    }

    [Fact]
    public async Task Handle_WithNullWizardFields_PreservesExistingValues()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var originalMinOverlapHours = job.MinOverlapHours;

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, null, null, false,
            City: null, Country: null, MinOverlapHours: null);

        await _handler.Handle(command, CancellationToken.None);

        job.MinOverlapHours.Should().Be(originalMinOverlapHours);
    }

    [Fact]
    public async Task Handle_WithWorkArrangementUpdate_UpdatesWorkArrangement()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null,
            WorkArrangement: WorkArrangement.Hybrid,
            null, null, null, false,
            City: "Dhaka", Country: "Bangladesh", MinOverlapHours: null);

        await _handler.Handle(command, CancellationToken.None);

        job.WorkArrangement.Should().Be(WorkArrangement.Hybrid);
    }

    [Fact]
    public async Task Handle_WhenPublishIsTrue_EnqueuesJobForJobAlerts()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        SetupVerifiedEmployer();

        var command = CommandFor(job.Id, publish: true);

        await _handler.Handle(command, CancellationToken.None);

        _jobAlertQueue.Verify(q => q.EnqueuePublishedJobAsync(job.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPublishIsFalse_DoesNotEnqueueJobForJobAlerts()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = CommandFor(job.Id, publish: false);

        await _handler.Handle(command, CancellationToken.None);

        _jobAlertQueue.Verify(q => q.EnqueuePublishedJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
