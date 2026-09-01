using FluentAssertions;
using FluentValidation;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly CreateJobCommandHandler _handler;

    public CreateJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _handler = new CreateJobCommandHandler(_jobRepository.Object, _unitOfWork.Object, _currentUserService.Object);
    }

    private static CreateJobCommand ValidCommand(bool publish = false) => new(
        "Backend Engineer",
        "Acme Corp",
        "Build and maintain backend services.",
        EmploymentType.FullTime,
        ExperienceLevel.MidLevel,
        "Dhaka",
        WorkArrangement.FullRemote,
        new List<string> { "C#", "SQL" },
        2,
        DateTime.UtcNow.AddDays(30),
        publish,
        City: null,
        Country: null,
        MinOverlapHours: 4
    );

    [Fact]
    public async Task Handle_PersistsANewDraftJob()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false);

        var jobId = await _handler.Handle(command, CancellationToken.None);

        addedJob.Should().NotBeNull();
        addedJob!.Id.Should().Be(jobId);
        addedJob.Status.Should().Be(JobStatus.Draft);
        addedJob.Title.Should().Be(command.Title);
        addedJob.CompanyName.Should().Be(command.CompanyName);
        addedJob.EmployerId.Should().Be("employer-1");
    }

    [Fact]
    public async Task Handle_CallsSaveChangesAsyncExactlyOnce()
    {
        var command = ValidCommand(publish: false);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndCompleteData_PublishesTheJob()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: true);

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndNoVacancies_ThrowsValidationException()
    {
        var command = ValidCommand(publish: true) with { Vacancies = 0 };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndNoVacancies_DoesNotPersistOrSave()
    {
        var command = ValidCommand(publish: true) with { Vacancies = 0 };

        try
        {
            await _handler.Handle(command, CancellationToken.None);
        }
        catch (FluentValidation.ValidationException)
        {
            // expected — assertions below verify no side effects occurred.
        }

        _jobRepository.Verify(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPublishFalseAndIncompleteData_StillPersistsAsDraft()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with { Vacancies = 0 };

        await _handler.Handle(command, CancellationToken.None);

        addedJob.Should().NotBeNull();
        addedJob!.Status.Should().Be(JobStatus.Draft);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var command = ValidCommand(publish: false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_DoesNotPersistOrSave()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var command = ValidCommand(publish: false);

        try
        {
            await _handler.Handle(command, CancellationToken.None);
        }
        catch (UnauthorizedException)
        {
            // expected — assertions below verify no side effects occurred.
        }

        _jobRepository.Verify(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithOnSiteWorkArrangement_SetsCityAndCountry()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = "Dhaka",
            Country = "Bangladesh",
            MinOverlapHours = null
        };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.City.Should().Be("Dhaka");
        addedJob.Country.Should().Be("Bangladesh");
        addedJob.MinOverlapHours.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithAnywhereFullRemote_SetsMinOverlapHours()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with
        {
            WorkArrangement = WorkArrangement.FullRemote,
            City = null,
            Country = null,
            MinOverlapHours = 6
        };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.MinOverlapHours.Should().Be(6);
        addedJob.City.Should().BeNull();
        addedJob.Country.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithHybridWorkArrangement_SetsCityCountryAndWorkArrangement()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with
        {
            WorkArrangement = WorkArrangement.Hybrid,
            City = "Chattogram",
            Country = "Bangladesh",
            MinOverlapHours = null
        };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.WorkArrangement.Should().Be(WorkArrangement.Hybrid);
        addedJob.City.Should().Be("Chattogram");
        addedJob.Country.Should().Be("Bangladesh");
    }

    [Fact]
    public async Task Handle_WithExpireAt_SetsExpireAtOnJob()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var expireDate = DateTime.UtcNow.AddDays(60);
        var command = ValidCommand(publish: false) with { ExpireAt = expireDate };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.ExpireAt.Should().Be(expireDate);
    }

    [Fact]
    public async Task Handle_WithVacancies_SetsVacanciesOnJob()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with { Vacancies = 5 };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.Vacancies.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NormalizesCityAndCountry()
    {
        Job? addedJob = null;
        _jobRepository
            .Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
            .Callback<Job, CancellationToken>((job, _) => addedJob = job)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(publish: false) with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = "  Dhaka  ",
            Country = "  Bangladesh  "
        };

        await _handler.Handle(command, CancellationToken.None);

        addedJob!.City.Should().Be("Dhaka");
        addedJob.Country.Should().Be("Bangladesh");
    }
}
