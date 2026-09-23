using FluentAssertions;
using Moq;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Auth.DeleteAccount;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.DeleteAccount;

public class SendDeleteAccountOtpCommandHandlerTests
{
    private const string Email = "user@example.com";
    private const string IdentityUserId = "identity-1";
    private const string IssuedOtp = "123456";

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly FakeOtpAttemptTracker _attemptTracker = new();
    private readonly FakeOtpCooldownTracker _cooldownTracker = new();
    private readonly SendDeleteAccountOtpCommandHandler _handler;
    private readonly string _otpKey = OtpKeys.DeleteAccount(Email);

    public SendDeleteAccountOtpCommandHandlerTests()
    {
        _currentUserServiceMock.SetupGet(c => c.Email).Returns(Email);
        _currentUserServiceMock.SetupGet(c => c.UserId).Returns(IdentityUserId);

        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);

        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(IdentityUserId, "Test User", Email, UserRole.Talent));

        _otpServiceMock
            .Setup(o => o.GenerateOtpAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IssuedOtp);

        _handler = new SendDeleteAccountOtpCommandHandler(
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _otpServiceMock.Object,
            _attemptTracker,
            _cooldownTracker,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldIssueOtpUnderDeleteAccountKeyAndEmailIt()
    {
        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(_otpKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailServiceMock.Verify(
            e => e.SendOtpEmailAsync(Email, "Test User", IssuedOtp, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotConsumeTheSignupEmailVerificationSession()
    {
        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.ConsumeEmailVerificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AfterLockoutFromFailedAttempts_ShouldUnblockVerification()
    {
        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts; attempt++)
        {
            await _attemptTracker.RecordFailureAsync(_otpKey);
        }

        (await _attemptTracker.IsBlockedAsync(_otpKey)).Should().BeTrue();

        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        (await _attemptTracker.IsBlockedAsync(_otpKey)).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenDomainUserAlreadyGone_ShouldStillIssueOtpForRetry()
    {
        // Identity row survives a failed cleanup, so the user must be able to request
        // a fresh code to finish the deletion.
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(_otpKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailServiceMock.Verify(
            e => e.SendOtpEmailAsync(Email, Email, IssuedOtp, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalledTwiceWithinCooldown_ShouldThrowValidationExceptionAndBlockSecondOtp()
    {
        // First send succeeds and initiates cooldown
        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(_otpKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailServiceMock.Verify(
            e => e.SendOtpEmailAsync(Email, "Test User", IssuedOtp, It.IsAny<CancellationToken>()),
            Times.Once);

        // Advance 15 seconds (still within 60s cooldown)
        _cooldownTracker.AdvanceTime(TimeSpan.FromSeconds(15));

        // Second send must be rejected
        var act = () => _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Contain("Please wait 45 seconds before requesting a new verification code.");

        // Verification service & email service should not have been called again
        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(_otpKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailServiceMock.Verify(
            e => e.SendOtpEmailAsync(Email, "Test User", IssuedOtp, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AfterCooldownExpires_ShouldAllowSendingNewOtp()
    {
        // First send
        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        // Advance past 60s cooldown
        _cooldownTracker.AdvanceTime(TimeSpan.FromSeconds(61));

        // Second send should succeed
        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(_otpKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _emailServiceMock.Verify(
            e => e.SendOtpEmailAsync(Email, "Test User", IssuedOtp, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
