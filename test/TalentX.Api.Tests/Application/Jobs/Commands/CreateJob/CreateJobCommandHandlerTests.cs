using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Api.Tests.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateJobCommandHandler _handler;

    public CreateJobCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobRepository.Object);
        _handler = new CreateJobCommandHandler(_unitOfWork.Object);
    }

    private static CreateJobCommand ValidCommand(bool publish = false) => new(
        "Backend Engineer",
        "Acme Corp",
        "Build and maintain backend services.",
        EmploymentType.FullTime,
        ExperienceLevel.MidLevel,
        "Dhaka",
        WorkArrangement.Remote,
        new List<string> { "C#", "SQL" },
        2,
        DateTime.UtcNow.AddDays(30),
        publish
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

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndNoVacancies_DoesNotPersistOrSave()
    {
        var command = ValidCommand(publish: true) with { Vacancies = 0 };

        try
        {
            await _handler.Handle(command, CancellationToken.None);
        }
        catch (ValidationException)
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
}
