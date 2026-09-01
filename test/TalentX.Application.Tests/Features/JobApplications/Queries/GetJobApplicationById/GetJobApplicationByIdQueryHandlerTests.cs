using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetJobApplicationById;

public class GetJobApplicationByIdQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly GetJobApplicationByIdQueryHandler _handler;

    public GetJobApplicationByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _presignedUrlService.Setup(p => p.CreateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://s3.amazonaws.com/test-bucket/resumes/abc_cv.pdf?signature");
        _handler = new GetJobApplicationByIdQueryHandler(_unitOfWork.Object, _currentUserService.Object, _presignedUrlService.Object);
    }

    private static Job JobOwnedBy(string employerId)
    {
        return Job.Create(
            employerId,
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
    }

    private static JobApplication ApplicationBy(string talentUserId, Guid jobId)
    {
        return new JobApplication(
            jobId,
            talentUserId,
            "Jane Talent",
            "jane@example.com",
            "resumes/abc_cv.pdf",
            "I am a great fit.",
            90000m,
            5m,
            "Dhaka");
    }

    [Fact]
    public async Task Handle_WhenCallerIsTheApplicant_ReturnsTheDto_WithoutLookingUpTheJob()
    {
        var jobId = Guid.NewGuid();
        var application = ApplicationBy("talent-1", jobId);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var result = await _handler.Handle(new GetJobApplicationByIdQuery(application.Id), CancellationToken.None);

        result.Id.Should().Be(application.Id);
        result.JobId.Should().Be(jobId);
        result.ApplicantName.Should().Be("Jane Talent");
        result.ApplicantEmail.Should().Be("jane@example.com");
        result.ResumeStorageKey.Should().Be("resumes/abc_cv.pdf");
        result.CoverLetter.Should().Be("I am a great fit.");
        result.ExpectedSalary.Should().Be(90000m);
        result.TotalYearsExperience.Should().Be(5m);
        result.Location.Should().Be("Dhaka");
        result.Status.Should().Be("Applied");
        result.AppliedAt.Should().Be(application.AppliedAt);
        result.UpdatedAt.Should().BeNull();

        _jobRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsTheEmployerWhoOwnsTheJob_ReturnsTheDto()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        var job = JobOwnedBy("employer-1");
        var application = ApplicationBy("talent-1", job.Id);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetJobApplicationByIdQuery(application.Id), CancellationToken.None);

        result.Id.Should().Be(application.Id);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNeitherApplicantNorJobOwner_ThrowsForbiddenException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("someone-else");
        var job = JobOwnedBy("employer-1");
        var application = ApplicationBy("talent-1", job.Id);
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _jobRepository
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var act = () => _handler.Handle(new GetJobApplicationByIdQuery(application.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ThrowsNotFoundException()
    {
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        var act = () => _handler.Handle(new GetJobApplicationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobRowIsMissingForANonApplicantCaller_ThrowsForbiddenException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        var application = ApplicationBy("talent-1", Guid.NewGuid());
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _jobRepository
            .Setup(r => r.GetByIdAsync(application.JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new GetJobApplicationByIdQuery(application.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetJobApplicationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_MapsNullOptionalFieldsThrough()
    {
        var jobId = Guid.NewGuid();
        var application = new JobApplication(
            jobId, "talent-1", "Jane Talent", "jane@example.com", "resumes/abc_cv.pdf");
        _jobApplicationRepository
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var result = await _handler.Handle(new GetJobApplicationByIdQuery(application.Id), CancellationToken.None);

        result.CoverLetter.Should().BeNull();
        result.ExpectedSalary.Should().BeNull();
        result.TotalYearsExperience.Should().BeNull();
        result.Location.Should().BeNull();
    }
}
