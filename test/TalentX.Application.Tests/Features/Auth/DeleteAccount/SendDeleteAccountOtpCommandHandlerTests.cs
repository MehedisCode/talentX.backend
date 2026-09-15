using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly SendDeleteAccountOtpCommandHandler _handler;
    private readonly string _otpKey = OtpKeys.DeleteAccount(Email);

    public SendDeleteAccountOtpCommandHandlerTests()
    {
        _currentUserServiceMock.SetupGet(c => c.Email).Returns(Email);
        _currentUserServiceMock.SetupGet(c => c.UserId).Returns(IdentityUserId);

        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId))
            .ReturnsAsync(new User(IdentityUserId, "Test User", Email, UserRole.Talent));

        _otpServiceMock
            .Setup(o => o.GenerateOtpAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IssuedOtp);

        _handler = new SendDeleteAccountOtpCommandHandler(
            _currentUserServiceMock.Object,
            _userRepositoryMock.Object,
            _otpServiceMock.Object,
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

        // The delete flow never marks an email verified, so consuming that store
        // would only destroy a pending signup session owned by another flow.
        _otpServiceMock.Verify(
            o => o.ConsumeEmailVerificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AfterLockoutFromFailedAttempts_ShouldUnblockVerification()
    {
        for (var attempt = 0; attempt < DeleteAccountOtpAttemptGuard.MaxFailedAttempts; attempt++)
        {
            DeleteAccountOtpAttemptGuard.RecordFailure(_otpKey);
        }

        DeleteAccountOtpAttemptGuard.IsBlocked(_otpKey).Should().BeTrue();

        await _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        DeleteAccountOtpAttemptGuard.IsBlocked(_otpKey).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithMissingEmail_ShouldThrowUnauthorized()
    {
        _currentUserServiceMock.SetupGet(c => c.Email).Returns((string?)null);

        var act = () => _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingUserId_ShouldThrowUnauthorized()
    {
        _currentUserServiceMock.SetupGet(c => c.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _otpServiceMock.Verify(
            o => o.GenerateOtpAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDomainUserMissing_ShouldThrowNotFound()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUserIdAsync(IdentityUserId))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new SendDeleteAccountOtpCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
