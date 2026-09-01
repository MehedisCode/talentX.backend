using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Commands.ApplyToJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.ApplyToJob;

public class ApplyToJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ApplyToJobCommandHandler _handler;

    public ApplyToJobCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _handler = new ApplyToJobCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static Job PublishedJob()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Remote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    private static ApplyToJobCommand ValidCommand(Guid jobId) => new(
        jobId,
        "  Jane Talent  ",
        "jane@example.com",
        "resumes/abc_cv.pdf",
        "  I am a great fit.  ",
        90000m,
        5m,
        "  Dhaka  ");

    [Fact]
    public async Task Handle_PersistsApplicationWithAppliedStatusAndTrimmedSnapshotValues()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(job.Id, "talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JobApplication? added = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((application, _) => added = application)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(ValidCommand(job.Id), CancellationToken.None);

        added.Should().NotBeNull();
        added!.JobId.Should().Be(job.Id);
        added.TalentUserId.Should().Be("talent-1");
        added.ApplicantName.Should().Be("Jane Talent");
        added.ApplicantEmail.Should().Be("jane@example.com");
        added.ResumeStorageKey.Should().Be("resumes/abc_cv.pdf");
        added.CoverLetter.Should().Be("I am a great fit.");
        added.ExpectedSalary.Should().Be(90000m);
        added.TotalYearsExperience.Should().Be(5m);
        added.Location.Should().Be("Dhaka");
        added.Status.Should().Be(ApplicationStatus.Applied);

        result.Id.Should().Be(added.Id);
        result.JobId.Should().Be(job.Id);
        result.Status.Should().Be("Applied");
        result.AppliedAt.Should().Be(added.AppliedAt);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TreatsBlankOptionalFieldsAsNull()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(job.Id, "talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JobApplication? added = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((application, _) => added = application)
            .Returns(Task.CompletedTask);

        var command = ValidCommand(job.Id) with { CoverLetter = "   ", Location = "" };

        await _handler.Handle(command, CancellationToken.None);

        added!.CoverLetter.Should().BeNull();
        added.Location.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LinksApplicationToTheCallerFromTheToken_NotAnythingInTheRequest()
    {
        var job = PublishedJob();
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-from-token");
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(job.Id, "talent-from-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JobApplication? added = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((application, _) => added = application)
            .Returns(Task.CompletedTask);

        await _handler.Handle(ValidCommand(job.Id), CancellationToken.None);

        added!.TalentUserId.Should().Be("talent-from-token");
    }

    [Fact]
    public async Task Handle_WhenJobIsNotOpen_ThrowsNotFoundException_AndDoesNotPersist()
    {
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _jobApplicationRepository.Verify(
            r => r.ExistsForJobAndTalentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTalentHasAlreadyApplied_ThrowsConflictException_AndDoesNotPersist()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(job.Id, "talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(ValidCommand(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
}
