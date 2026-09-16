using AutoMapper;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.DTOs.Auth;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.GoogleSignIn;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers;

public sealed class GoogleSignInEmployerCommandHandlerTests
{
    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock = new();
    private readonly Mock<IExternalIdentityService> _externalIdentityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<ITalentRepository> _talentRepositoryMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    public GoogleSignInEmployerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Companies).Returns(_companyRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Talents).Returns(_talentRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditLogs).Returns(_auditLogRepositoryMock.Object);

        _mapperMock
            .Setup(m => m.Map<EmployerDto>(It.IsAny<Employer>()))
            .Returns((Employer e) => new EmployerDto(
                e.Id,
                e.UserId,
                e.Name,
                e.Email,
                e.CompanyName,
                e.Role,
                e.IsVerified,
                e.VerificationStatus,
                e.CreatedAt,
                e.UpdatedAt
            ));

        _tokenServiceMock
            .Setup(t => t.CreateAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "mock-access-token",
                RefreshToken = "mock-refresh-token"
            }));
    }

    [Fact]
    public async Task Handle_SuccessfullyCreatesNewEmployer_AndReturnsTokens_WhenGoogleTokenIsValid()
    {
        // Arrange
        var command = new GoogleSignInEmployerCommand("valid-google-id-token");
        var googleUser = new GoogleUserInfo("google-sub-123", "employer@google.com", "Google Employer");

        _googleAuthServiceMock
            .Setup(g => g.VerifyIdTokenAsync(command.IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUser);

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(googleUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        _talentRepositoryMock
            .Setup(r => r.GetByUserIdAsync("identity-user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _talentRepositoryMock
            .Setup(r => r.GetByEmailAsync(googleUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _externalIdentityServiceMock
            .Setup(e => e.FindOrCreateExternalUserAsync(googleUser.Email, "Employer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "identity-user-123"));

        var handler = new GoogleSignInEmployerCommandHandler(
            _googleAuthServiceMock.Object,
            _externalIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Employer);
        Assert.Equal(googleUser.Email, result.Employer.Email);
        Assert.Equal(googleUser.Name, result.Employer.Name);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.Equal("mock-refresh-token", result.RefreshToken);

        _employerRepositoryMock.Verify(r => r.AddAsync(It.Is<Employer>(e => e.Email == googleUser.Email && e.IsVerified), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepositoryMock.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Actor == googleUser.Email && l.Action == "Login Success · Google"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _tokenServiceMock.Verify(t => t.CreateAsync("identity-user-123"), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsExistingEmployer_AndTokens_WhenExistingUnverified()
    {
        // Arrange
        var command = new GoogleSignInEmployerCommand("valid-google-id-token");
        var googleUser = new GoogleUserInfo("google-sub-123", "existing@google.com", "Existing Employer");
        var existingEmployer = new Employer("existing-user-id", "Existing Employer", "existing@google.com", "My Org");

        _googleAuthServiceMock
            .Setup(g => g.VerifyIdTokenAsync(command.IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUser);

        _talentRepositoryMock
            .Setup(r => r.GetByUserIdAsync("existing-user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _talentRepositoryMock
            .Setup(r => r.GetByEmailAsync(googleUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Talent?)null);

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(googleUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployer);

        _externalIdentityServiceMock
            .Setup(e => e.FindOrCreateExternalUserAsync(googleUser.Email, "Employer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "existing-user-id"));

        var handler = new GoogleSignInEmployerCommandHandler(
            _googleAuthServiceMock.Object,
            _externalIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Employer);
        Assert.True(existingEmployer.IsVerified);
        Assert.Equal("existing@google.com", result.Employer.Email);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.Equal("mock-refresh-token", result.RefreshToken);
        _externalIdentityServiceMock.Verify(e => e.FindOrCreateExternalUserAsync(googleUser.Email, "Employer", It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepositoryMock.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Actor == googleUser.Email && l.Action == "Login Success · Google"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _tokenServiceMock.Verify(t => t.CreateAsync("existing-user-id"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingUserIsTalent_ReturnsTokensWithNullEmployer_AndDoesNotCreateEmployer()
    {
        // Arrange
        var command = new GoogleSignInEmployerCommand("valid-google-id-token");
        var googleUser = new GoogleUserInfo("google-sub-123", "talent@google.com", "Talent Candidate");
        var talent = new Talent("talent-user-id", "Talent Candidate", "talent@google.com");

        _googleAuthServiceMock
            .Setup(g => g.VerifyIdTokenAsync(command.IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUser);

        _externalIdentityServiceMock
            .Setup(e => e.FindOrCreateExternalUserAsync(googleUser.Email, "Employer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "talent-user-id"));

        _talentRepositoryMock
            .Setup(r => r.GetByUserIdAsync("talent-user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(talent);

        var handler = new GoogleSignInEmployerCommandHandler(
            _googleAuthServiceMock.Object,
            _externalIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Employer);
        Assert.True(talent.IsVerified);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.Equal("mock-refresh-token", result.RefreshToken);

        _employerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Employer>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditLogRepositoryMock.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Actor == googleUser.Email && l.Action == "Login Success · Google"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _tokenServiceMock.Verify(t => t.CreateAsync("talent-user-id"), Times.Once);
    }
}
