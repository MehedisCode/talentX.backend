using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.Commands.VerifyGuestEmail;

public class VerifyGuestEmailCommandHandlerTests
{
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobAppRepository = new();
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ILogger<VerifyGuestEmailCommandHandler>> _logger = new();

    private readonly VerifyGuestEmailCommandHandler _handler;

    public VerifyGuestEmailCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.AuditLogs).Returns(_auditLogRepository.Object);
        _tokenService
            .Setup(t => t.CreateAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "access-token-123",
                RefreshToken = "refresh-token-123"
            }));

        _handler = new VerifyGuestEmailCommandHandler(
            _tokenRepository.Object,
            _jobAppRepository.Object,
            _identityService.Object,
            _talentRepository.Object,
            _tokenService.Object,
            _emailService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WithNewGuest_CreatesUserAndTalent_SendsCredentials_SetsStatusApplied()
    {
        // Arrange
        const string token = "valid-token";
        const string email = "guest@example.com";
        var applicationId = Guid.NewGuid();
        var tokenEntity = new EmailVerificationToken(token, email, applicationId, TimeSpan.FromMinutes(30));
        var application = JobApplication.CreateGuestApplication(Guid.NewGuid(), "Guest User", email, "resumes/r.pdf");

        _tokenRepository
            .Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        _jobAppRepository
            .Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _talentRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _identityService
            .Setup(i => i.CreateApplicationUserAsync(email, It.IsAny<string>(), UserRole.Guest))
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-new-1" }));

        _jobAppRepository
            .Setup(r => r.GetPendingByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplication> { application });

        // Act
        var result = await _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        // Assert
        result.UserId.Should().Be("user-new-1");
        result.Email.Should().Be(email);
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-123");

        application.TalentUserId.Should().Be("user-new-1");
        application.Status.Should().Be(ApplicationStatus.Applied);
        tokenEntity.IsConsumed.Should().BeTrue();

        _talentRepository.Verify(
            r => r.AddAsync(It.Is<Talent>(t => t.UserId == "user-new-1" && t.Email == email), It.IsAny<CancellationToken>()),
            Times.Once);

        _emailService.Verify(
            e => e.SendCredentialsEmailAsync(email, "Guest User", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingTalent_DoesNotOverwritePassword_DoesNotSendCredentials()
    {
        // Arrange
        const string token = "valid-token";
        const string email = "existing@example.com";
        var applicationId = Guid.NewGuid();
        var tokenEntity = new EmailVerificationToken(token, email, applicationId, TimeSpan.FromMinutes(30));
        var application = JobApplication.CreateGuestApplication(Guid.NewGuid(), "Existing Talent", email, "resumes/r.pdf");
        var existingTalent = new Talent("talent-user-existing", "Existing Talent", email);

        _tokenRepository
            .Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        _jobAppRepository
            .Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _talentRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTalent);

        _jobAppRepository
            .Setup(r => r.GetPendingByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplication> { application });

        // Act
        var result = await _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        // Assert
        result.UserId.Should().Be("talent-user-existing");
        result.Email.Should().Be(email);
        application.TalentUserId.Should().Be("talent-user-existing");
        application.Status.Should().Be(ApplicationStatus.Applied);
        tokenEntity.IsConsumed.Should().BeTrue();

        // Must NOT create user or talent
        _identityService.Verify(
            i => i.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()),
            Times.Never);
        _talentRepository.Verify(
            r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Must NOT change password or send temporary credentials
        _identityService.Verify(
            i => i.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _emailService.Verify(
            e => e.SendCredentialsEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOtherPendingApplications_LinksThemAndMarksApplied_WithoutRejecting()
    {
        // Arrange
        const string token = "valid-token";
        const string email = "applicant@example.com";
        var applicationId = Guid.NewGuid();
        var tokenEntity = new EmailVerificationToken(token, email, applicationId, TimeSpan.FromMinutes(30));
        var app1 = JobApplication.CreateGuestApplication(Guid.NewGuid(), "Applicant", email, "resumes/r1.pdf");
        var app2 = JobApplication.CreateGuestApplication(Guid.NewGuid(), "Applicant", email, "resumes/r2.pdf");

        _tokenRepository
            .Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        _jobAppRepository
            .Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app1);

        _talentRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _identityService
            .Setup(i => i.CreateApplicationUserAsync(email, It.IsAny<string>(), UserRole.Guest))
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-multi-1" }));

        _jobAppRepository
            .Setup(r => r.GetPendingByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplication> { app1, app2 });

        // Act
        var result = await _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        // Assert
        result.UserId.Should().Be("user-multi-1");

        // Primary application is linked and applied
        app1.TalentUserId.Should().Be("user-multi-1");
        app1.Status.Should().Be(ApplicationStatus.Applied);

        // Second application MUST NOT be Rejected — it should be linked and applied!
        app2.Status.Should().NotBe(ApplicationStatus.Rejected);
        app2.Status.Should().Be(ApplicationStatus.Applied);
        app2.TalentUserId.Should().Be("user-multi-1");
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsNotFoundException()
    {
        _tokenRepository
            .Setup(r => r.GetByTokenAsync("unknown-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationToken?)null);

        var act = () => _handler.Handle(new VerifyGuestEmailCommand("unknown-token"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Invalid verification link.");
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ThrowsValidationException()
    {
        const string token = "expired-token";
        var tokenEntity = new EmailVerificationToken(token, "test@example.com", Guid.NewGuid(), TimeSpan.FromSeconds(-10));

        _tokenRepository
            .Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        var act = () => _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyConsumed_ThrowsValidationException()
    {
        const string token = "consumed-token";
        var tokenEntity = new EmailVerificationToken(token, "test@example.com", Guid.NewGuid(), TimeSpan.FromMinutes(30));
        tokenEntity.MarkConsumed();

        _tokenRepository
            .Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);

        var act = () => _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already been used*");
    }
}
