using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IEmailVerificationTokenRepository> _verificationTokenRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<ApplyToJobCommandHandler>> _logger = new();
    private readonly ApplyToJobCommandHandler _handler;

    public ApplyToJobCommandHandlerTests()
    {
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IUploadedFile _, string key, CancellationToken _) => key);

        _unitOfWork
            .Setup(u => u.Users)
            .Returns(_userRepository.Object);

        _handler = new ApplyToJobCommandHandler(
            _jobRepository.Object,
            _jobApplicationRepository.Object,
            _verificationTokenRepository.Object,
            _emailService.Object,
            _fileStorageService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private static Job PublishedJob(WorkArrangement arrangement = WorkArrangement.RemoteButAccessible) =>
        Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            arrangement,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );

    private static Mock<IUploadedFile> MockResumeFile(string fileName = "resume.pdf")
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([0x25, 0x50, 0x44, 0x46]));
        return fileMock;
    }

    private static ApplyToJobCommand ValidCommand(string? location = "Remote - anywhere", IUploadedFile? file = null) => new(
        Guid.NewGuid(),
        "Jane Doe",
        "Jane@Example.com ",
        file ?? MockResumeFile().Object,
        "I am excited to apply for this role.",
        85000,
        6,
        location
    );

    private void SetupPublishedJob(WorkArrangement arrangement = WorkArrangement.RemoteButAccessible)
    {
        var job = PublishedJob(arrangement);
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    [Fact]
    public async Task Handle_WithNewEmail_SavesApplicationWithPendingVerificationAndSendsVerificationLink()
    {
        SetupPublishedJob();
        _jobApplicationRepository
            .Setup(r => r.ExistsPendingByJobIdAndEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JobApplication? addedApplication = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((app, _) => addedApplication = app)
            .Returns(Task.CompletedTask);

        EmailVerificationToken? addedToken = null;
        _verificationTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationToken, CancellationToken>((token, _) => addedToken = token)
            .Returns(Task.CompletedTask);

        var command = ValidCommand();
        var response = await _handler.Handle(command, CancellationToken.None);

        addedApplication.Should().NotBeNull();
        addedApplication!.JobId.Should().Be(command.JobId);
        addedApplication.TalentUserId.Should().BeNull();
        addedApplication.ApplicantName.Should().Be("Jane Doe");
        addedApplication.ApplicantEmail.Should().Be("jane@example.com");
        addedApplication.Status.Should().Be(ApplicationStatus.PendingVerification);
        addedApplication.ResumeStorageKey.Should().StartWith("resumes/pending/");
        addedApplication.ResumeStorageKey.Should().EndWith("resume.pdf");

        addedToken.Should().NotBeNull();
        addedToken!.ApplicantEmail.Should().Be("jane@example.com");
        addedToken.JobApplicationId.Should().Be(addedApplication.Id);
        addedToken.IsValid.Should().BeTrue();

        _emailService.Verify(
            e => e.SendVerificationLinkEmailAsync(
                "jane@example.com",
                "Jane Doe",
                It.Is<string>(url => url.Contains(addedToken.Token)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        response.ApplicationId.Should().Be(addedApplication.Id);
        response.JobId.Should().Be(command.JobId);
    }

    [Fact]
    public async Task Handle_WhenPendingApplicationExistsForEmail_ResendsVerificationEmail()
    {
        SetupPublishedJob();
        var existingApp = JobApplication.CreateGuestApplication(
            Guid.NewGuid(), "Jane Doe", "jane@example.com", "resumes/old.pdf");
        _jobApplicationRepository
            .Setup(r => r.GetPendingByJobIdAndEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        EmailVerificationToken? addedToken = null;
        _verificationTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
            .Callback<EmailVerificationToken, CancellationToken>((token, _) => addedToken = token)
            .Returns(Task.CompletedTask);

        var command = ValidCommand();
        var response = await _handler.Handle(command, CancellationToken.None);

        addedToken.Should().NotBeNull();
        addedToken!.JobApplicationId.Should().Be(existingApp.Id);

        _emailService.Verify(
            e => e.SendVerificationLinkEmailAsync(
                "jane@example.com",
                "Jane Doe",
                It.Is<string>(url => url.Contains(addedToken.Token)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        response.ApplicationId.Should().Be(existingApp.Id);
        response.VerificationEmailResent.Should().BeTrue();

        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobIsNotFound_ThrowsNotFoundException()
    {
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = ValidCommand();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonRemoteJobAndMissingLocation_ThrowsValidationException()
    {
        var job = PublishedJob(WorkArrangement.OnSite);
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var command = ValidCommand(location: null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRemoteJobAndMissingLocation_Succeeds()
    {
        SetupPublishedJob(WorkArrangement.RemoteButAccessible);
        _jobApplicationRepository
            .Setup(r => r.ExistsPendingByJobIdAndEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = ValidCommand(location: null);
        var response = await _handler.Handle(command, CancellationToken.None);

        response.ApplicationId.Should().NotBeEmpty();
        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.Is<JobApplication>(a => a.Location == null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFileStorageFails_ThrowsBadGatewayException()
    {
        SetupPublishedJob();
        _jobApplicationRepository
            .Setup(r => r.ExistsPendingByJobIdAndEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fileStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Storage unavailable"));

        var command = ValidCommand();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAsTalent_ReturnsRequiresLogin()
    {
        SetupPublishedJob();
        _userRepository
            .Setup(r => r.GetByEmailAsync("jane@example.com"))
            .ReturnsAsync(new User("user-1", "Jane Doe", "jane@example.com", UserRole.Talent));

        var command = ValidCommand();
        var response = await _handler.Handle(command, CancellationToken.None);

        response.RequiresLogin.Should().BeTrue();
        response.ApplicationId.Should().BeEmpty();
        _jobApplicationRepository.Verify(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAsGuest_ReturnsRequiresLogin()
    {
        SetupPublishedJob();
        _userRepository
            .Setup(r => r.GetByEmailAsync("jane@example.com"))
            .ReturnsAsync(new User("user-1", "Jane Doe", "jane@example.com", UserRole.Guest));

        var command = ValidCommand();
        var response = await _handler.Handle(command, CancellationToken.None);

        response.RequiresLogin.Should().BeTrue();
        response.ApplicationId.Should().BeEmpty();
        _jobApplicationRepository.Verify(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAsEmployer_ThrowsConflictException()
    {
        SetupPublishedJob();
        _userRepository
            .Setup(r => r.GetByEmailAsync("jane@example.com"))
            .ReturnsAsync(new User("user-1", "Jane Doe", "jane@example.com", UserRole.Employer));

        var command = ValidCommand();
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already registered as Employer*");
        _jobApplicationRepository.Verify(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
