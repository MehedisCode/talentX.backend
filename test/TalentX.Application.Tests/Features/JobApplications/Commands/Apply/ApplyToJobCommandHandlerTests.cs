using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ApplyToJobCommandHandler _handler;

    public ApplyToJobCommandHandlerTests()
    {
        _handler = new ApplyToJobCommandHandler(
            _jobRepository.Object,
            _talentRepository.Object,
            _jobApplicationRepository.Object,
            _identityService.Object,
            _emailService.Object,
            _unitOfWork.Object);
    }

    private static Job PublishedJob(WorkArrangement arrangement = WorkArrangement.Remote) =>
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

    private static ApplyToJobCommand ValidCommand(string? location = "Remote - anywhere") => new(
        Guid.NewGuid(),
        "Jane Doe",
        "Jane@Example.com ",
        "uploads/1234/resume.pdf",
        "I am excited to apply for this role.",
        85000,
        6,
        location
    );

    private void SetupPublishedJob(WorkArrangement arrangement = WorkArrangement.Remote)
    {
        var job = PublishedJob(arrangement);
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    [Fact]
    public async Task Handle_WithNewEmail_CreatesAccountAddsTalentAndApplicationAndEmailsCredentials()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);
        _identityService
            .Setup(s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-1" }));

        Talent? addedTalent = null;
        _talentRepository
            .Setup(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()))
            .Callback<Talent, CancellationToken>((talent, _) => addedTalent = talent)
            .Returns(Task.CompletedTask);

        JobApplication? addedApplication = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((app, _) => addedApplication = app)
            .Returns(Task.CompletedTask);

        var command = ValidCommand();
        var response = await _handler.Handle(command, CancellationToken.None);

        _identityService.Verify(
            s => s.CreateApplicationUserAsync(
                "jane@example.com",
                It.IsAny<string>(),
                UserRole.Talent),
            Times.Once);

        addedTalent.Should().NotBeNull();
        addedTalent!.UserId.Should().Be("user-1");
        addedTalent.Email.Should().Be("jane@example.com");
        addedTalent.Role.Should().Be(UserRole.Talent);

        addedApplication.Should().NotBeNull();
        addedApplication!.JobId.Should().Be(command.JobId);
        addedApplication.TalentUserId.Should().Be("user-1");
        addedApplication.ApplicantName.Should().Be("Jane Doe");
        addedApplication.ApplicantEmail.Should().Be("jane@example.com");
        addedApplication.Status.Should().Be(ApplicationStatus.Applied);
        addedApplication.ResumeStorageKey.Should().Be(command.ResumeStorageKey);
        addedApplication.ExpectedSalary.Should().Be(command.ExpectedSalary);
        addedApplication.Location.Should().Be(command.Location);

        _emailService.Verify(
            e => e.SendCredentialsEmailAsync("jane@example.com", "Jane Doe", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        response.ApplicationId.Should().Be(addedApplication.Id);
        response.JobId.Should().Be(command.JobId);
        response.TalentAccountCreated.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithExistingTalent_LinksApplicationWithoutCreatingAccountOrEmailing()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        var existing = new Talent("talent-user-1", "Jane Doe", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JobApplication? addedApplication = null;
        _jobApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((app, _) => addedApplication = app)
            .Returns(Task.CompletedTask);

        var command = ValidCommand();

        var response = await _handler.Handle(command, CancellationToken.None);

        _identityService.Verify(
            s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()),
            Times.Never);
        _talentRepository.Verify(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailService.Verify(
            e => e.SendCredentialsEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        addedApplication.Should().NotBeNull();
        addedApplication!.TalentUserId.Should().Be("talent-user-1");
        addedApplication.ApplicantEmail.Should().Be("jane@example.com");
        addedApplication.Status.Should().Be(ApplicationStatus.Applied);

        response.TalentAccountCreated.Should().BeFalse();
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
        _identityService.Verify(
            s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonRemoteJobAndMissingLocation_ThrowsValidationException()
    {
        var job = PublishedJob(WorkArrangement.OnSite);
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        var command = ValidCommand(location: null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        _identityService.Verify(
            s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRemoteJobAndMissingLocation_Succeeds()
    {
        var job = PublishedJob(WorkArrangement.Remote);
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);
        _identityService
            .Setup(s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-1" }));

        var command = ValidCommand(location: null);
        var response = await _handler.Handle(command, CancellationToken.None);

        response.TalentAccountCreated.Should().BeTrue();
        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.Is<JobApplication>(a => a.Location == null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyApplied_ThrowsConflictExceptionWithoutCreatingAccount()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        var existing = new Talent("talent-user-1", "Jane Doe", "jane@example.com");
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _jobApplicationRepository
            .Setup(r => r.ExistsForJobAndTalentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = ValidCommand();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _identityService.Verify(
            s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()),
            Times.Never);
        _jobApplicationRepository.Verify(
            r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityAccountCreationFails_ThrowsConflictExceptionAndDoesNotSave()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);
        _identityService
            .Setup(s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Fail("Email is already in use."));

        var command = ValidCommand();

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _talentRepository.Verify(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()), Times.Never);
        _jobApplicationRepository.Verify(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNewEmail_AppliesPasswordThatMeetsIdentityPolicy()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _talentRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        string? generatedPassword = null;
        _identityService
            .Setup(s => s.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()))
            .Callback<string, string, UserRole>((_, password, _) => generatedPassword = password)
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-1" }));

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        generatedPassword.Should().NotBeNull();
        generatedPassword!.Length.Should().Be(12);
        generatedPassword.Should().MatchRegex("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12}$");
    }
}