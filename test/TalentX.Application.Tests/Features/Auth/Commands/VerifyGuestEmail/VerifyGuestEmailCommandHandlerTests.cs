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
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IJobApplicationStatusHistoryRepository> _historyRepository = new();
    private readonly Mock<ILogger<VerifyGuestEmailCommandHandler>> _logger = new();

    private readonly VerifyGuestEmailCommandHandler _handler;

    public VerifyGuestEmailCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.AuditLogs).Returns(_auditLogRepository.Object);
        _unitOfWork.Setup(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.Setup(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _unitOfWork.Setup(u => u.Educations).Returns(_educationRepository.Object);
        _unitOfWork.Setup(u => u.Projects).Returns(_projectRepository.Object);
        _unitOfWork.Setup(u => u.JobApplicationStatusHistories).Returns(_historyRepository.Object);

        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume>());
        _workExperienceRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkExperience>());
        _educationRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Education>());
        _projectRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
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

    [Fact]
    public async Task Handle_WithNewGuest_WhenApplicationHasExtractedProjects_CreatesProjectsForGuest()
    {
        // Arrange
        const string token = "valid-token";
        const string email = "guest-projects@example.com";
        var applicationId = Guid.NewGuid();
        var tokenEntity = new EmailVerificationToken(token, email, applicationId, TimeSpan.FromMinutes(30));
        var projectsJson = """
        [
            {
                "projectName": "TalentX Platform",
                "role": "Backend Lead",
                "projectType": "Professional",
                "startDate": "2023",
                "endDate": null,
                "isOngoing": true,
                "description": "AI marketplace",
                "technologiesUsed": ["C#", ".NET", "PostgreSQL"],
                "projectUrl": "https://talentx.ai"
            }
        ]
        """;
        var application = JobApplication.CreateGuestApplication(
            Guid.NewGuid(), "Guest User", email, "resumes/r.pdf",
            extractedProjectsJson: projectsJson);

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
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-new-proj" }));

        _jobAppRepository
            .Setup(r => r.GetPendingByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplication> { application });

        // Act
        var result = await _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        // Assert
        result.UserId.Should().Be("user-new-proj");
        _projectRepository.Verify(
            r => r.AddAsync(
                It.Is<Project>(p => p.UserId == "user-new-proj" && p.ProjectName == "TalentX Platform" && p.Role == "Backend Lead"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewGuest_WhenApplicationHasExtractedProfile_PopulatesTalentProfileFields()
    {
        const string token = "valid-token";
        const string email = "guest-profile@example.com";
        var applicationId = Guid.NewGuid();
        var tokenEntity = new EmailVerificationToken(token, email, applicationId, TimeSpan.FromMinutes(30));
        var profileJson = """
        {
            "tagline": "Senior AI Architect",
            "shortBio": "Building next-generation intelligent platforms.",
            "phone": "+1-555-0199",
            "linkedInUrl": "https://linkedin.com/in/guestai",
            "portfolioUrls": ["https://github.com/guestai", "https://guest.dev"],
            "skillPropositions": ["Enterprise AI Architecture", "Distributed Systems"]
        }
        """;
        var application = JobApplication.CreateGuestApplication(
            Guid.NewGuid(),
            "Guest User",
            email,
            "resumes/r.pdf",
            location: "San Francisco, CA",
            expectedSalary: 160000,
            totalYearsExperience: 8,
            extractedProfileJson: profileJson);

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
            .ReturnsAsync(IdentityResponse<CreateUserResult>.Ok(new CreateUserResult { UserId = "user-new-profile" }));

        _jobAppRepository
            .Setup(r => r.GetPendingByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobApplication> { application });

        Talent? createdTalent = null;
        _talentRepository
            .Setup(r => r.AddAsync(It.IsAny<Talent>(), It.IsAny<CancellationToken>()))
            .Callback<Talent, CancellationToken>((talent, _) => createdTalent = talent)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new VerifyGuestEmailCommand(token), CancellationToken.None);

        result.UserId.Should().Be("user-new-profile");
        createdTalent.Should().NotBeNull();
        createdTalent!.Tagline.Should().Be("Senior AI Architect");
        createdTalent.ShortBio.Should().Be("Building next-generation intelligent platforms.");
        createdTalent.Location.Should().Be("San Francisco, CA");
        createdTalent.Phone.Should().Be("+1-555-0199");
        createdTalent.LinkedInUrl.Should().Be("https://linkedin.com/in/guestai");
        createdTalent.AdditionalUrls.Should().Equal("https://github.com/guestai", "https://guest.dev");
        createdTalent.SkillPropositions.Should().Equal("Enterprise AI Architecture", "Distributed Systems");
        createdTalent.TotalYearsExperience.Should().Be(8);
        createdTalent.ExpectedSalary.Should().Be(160000);
    }
}
