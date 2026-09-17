using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Auth.DeleteAccount;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;
using BadGatewayException = TalentX.Application.Common.Exceptions.BadGatewayException;
using ValidationException = TalentX.Application.Common.Exceptions.ValidationException;

namespace TalentX.Application.Tests.Features.Auth.DeleteAccount;

public class DeleteAccountCommandHandlerTests
{
    private const string Email = "user@example.com";
    private const string IdentityUserId = "identity-1";
    private const string ValidOtp = "123456";

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJobRepository> _jobRepositoryMock = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepositoryMock = new();
    private readonly Mock<IInterviewInvitationRepository> _interviewInvitationRepositoryMock = new();
    private readonly Mock<IInvitationRepository> _invitationRepositoryMock = new();
    private readonly Mock<IJobTalentMatchRepository> _jobTalentMatchRepositoryMock = new();
    private readonly Mock<ISavedJobRepository> _savedJobRepositoryMock = new();
    private readonly Mock<IResumeRepository> _resumeRepositoryMock = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepositoryMock = new();
    private readonly Mock<IEducationRepository> _educationRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<IJobModerationHistoryRepository> _jobModerationHistoryRepositoryMock = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepositoryMock = new();
    private readonly Mock<ITalentNotificationPreferenceRepository> _talentNotificationPreferenceRepositoryMock = new();
    private readonly Mock<IJobAlertNotificationHistoryRepository> _jobAlertNotificationHistoryRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<DeleteAccountCommandHandler>> _loggerMock = new();

    private readonly FakeOtpAttemptTracker _attemptTracker = new();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Jobs).Returns(_jobRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.InterviewInvitations).Returns(_interviewInvitationRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Invitations).Returns(_invitationRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.JobTalentMatches).Returns(_jobTalentMatchRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.SavedJobs).Returns(_savedJobRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Resumes).Returns(_resumeRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Educations).Returns(_educationRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Companies).Returns(_companyRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.JobModerationHistories).Returns(_jobModerationHistoryRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.JobAlerts).Returns(_jobAlertRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.TalentNotificationPreferences).Returns(_talentNotificationPreferenceRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.JobAlertNotificationHistories).Returns(_jobAlertNotificationHistoryRepositoryMock.Object);

        _currentUserServiceMock.SetupGet(c => c.Email).Returns(Email);
        _currentUserServiceMock.SetupGet(c => c.UserId).Returns(IdentityUserId);

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(OtpKeys.DeleteAccount(Email), ValidOtp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _identityServiceMock
            .Setup(i => i.DeleteApplicationUserAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse.Ok());
        _identityServiceMock
            .Setup(i => i.DeleteApplicationUserAsync("identity-fail"))
            .ReturnsAsync(IdentityResponse.Fail("identity store unavailable"));

        _tokenServiceMock
            .Setup(t => t.RevokeAllForUserAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse.Ok());

        _handler = new DeleteAccountCommandHandler(
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _attemptTracker,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    private static User NewUser(UserRole role) =>
        new(IdentityUserId, "Test User", Email, role);

    #region OTP verification

    [Fact]
    public async Task Handle_WithValidOtp_ShouldVerifyAndResetAttemptCounter()
    {
        var user = NewUser(UserRole.Talent);
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.VerifyOtpAsync(OtpKeys.DeleteAccount(Email), ValidOtp, It.IsAny<CancellationToken>()),
            Times.Once);
        (await _attemptTracker.IsBlockedAsync(OtpKeys.DeleteAccount(Email))).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldThrowAndRecordFailure()
    {
        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(OtpKeys.DeleteAccount(Email), "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(new DeleteAccountCommand("000000"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Invalid or expired verification code.");
        (await _attemptTracker.IsBlockedAsync(OtpKeys.DeleteAccount(Email))).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAttemptsExhausted_ShouldBlockFurtherVerification()
    {
        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(OtpKeys.DeleteAccount(Email), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts; attempt++)
        {
            var act = () => _handler.Handle(new DeleteAccountCommand("000000"), CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();
        }

        var blocked = () => _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);
        await blocked.Should().ThrowAsync<ValidationException>()
            .WithMessage("Too many incorrect verification attempts. Request a new code and try again.");

        _otpServiceMock.Verify(
            o => o.VerifyOtpAsync(OtpKeys.DeleteAccount(Email), ValidOtp, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Talent deletion

    [Fact]
    public async Task Handle_ForTalent_ShouldDeleteAllTalentOwnedRowsWithoutTouchingJobs()
    {
        var user = NewUser(UserRole.Talent);
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        result.Email.Should().Be(Email);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobRepositoryMock.Verify(j => j.RemoveRange(It.IsAny<IEnumerable<Job>>()), Times.Never);
        _jobRepositoryMock.Verify(j => j.GetByEmployerIdIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.Companies.DeleteByEmployerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.JobApplications.DeleteByTalentUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.InterviewInvitations.DeleteByTalentUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Invitations.DeleteByTalentUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.JobTalentMatches.DeleteByTalentUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SavedJobs.DeleteByTalentIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.JobAlerts.DeleteByTalentIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.TalentNotificationPreferences.DeleteByTalentIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.JobAlertNotificationHistories.DeleteByTalentIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Resumes.DeleteByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.WorkExperiences.DeleteByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Educations.DeleteByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.Delete(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _identityServiceMock.Verify(i => i.DeleteApplicationUserAsync(IdentityUserId), Times.Once);
        _tokenServiceMock.Verify(t => t.RevokeAllForUserAsync(IdentityUserId), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendAccountDeletedEmailAsync(Email, user.Name, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Employer deletion

    [Fact]
    public async Task Handle_ForEmployer_ShouldRemoveAllTheirJobsIncludingSoftDeleted()
    {
        var user = NewUser(UserRole.Employer);
        var jobs = new List<Job>
        {
            Job.Create(IdentityUserId, "Dev", "TestCo", "Desc",
                Domain.Enums.EmploymentType.FullTime, Domain.Enums.ExperienceLevel.MidLevel,
                "Loc", Domain.Enums.WorkArrangement.FullRemote,
                new List<string> { "C#" }, 1, DateTime.UtcNow.AddDays(30))
        };
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jobRepositoryMock
            .Setup(j => j.GetByEmployerIdIncludingDeletedAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        await _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        _jobRepositoryMock.Verify(j => j.GetByEmployerIdIncludingDeletedAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.JobModerationHistories.DeleteByJobIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _jobRepositoryMock.Verify(j => j.RemoveRange(jobs), Times.Once);
        _unitOfWorkMock.Verify(u => u.Companies.DeleteByEmployerIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _identityServiceMock.Verify(i => i.DeleteApplicationUserAsync(IdentityUserId), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ForEmployer_TalentScopedCleanupIsHarmlessNoOp()
    {
        var user = NewUser(UserRole.Employer);
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jobRepositoryMock
            .Setup(j => j.GetByEmployerIdIncludingDeletedAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        await _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.JobApplications.DeleteByTalentUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Resumes.DeleteByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Companies.DeleteByEmployerIdAsync(IdentityUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Convergence / failure paths

    [Fact]
    public async Task Handle_WhenUserAlreadyGone_ShouldFinishIdentityCleanupAndNotify()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        result.Email.Should().Be(Email);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _identityServiceMock.Verify(i => i.DeleteApplicationUserAsync(IdentityUserId), Times.Once);
        _tokenServiceMock.Verify(t => t.RevokeAllForUserAsync(IdentityUserId), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(
            e => e.SendAccountDeletedEmailAsync(Email, Email, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdentityDeleteFails_ShouldThrowBadGatewayException()
    {
        const string failingUserId = "identity-fail";
        _currentUserServiceMock.SetupGet(c => c.UserId).Returns(failingUserId);
        var user = NewUser(UserRole.Talent);
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(failingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => _handler.Handle(new DeleteAccountCommand(ValidOtp), CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("*could not be completed*");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
