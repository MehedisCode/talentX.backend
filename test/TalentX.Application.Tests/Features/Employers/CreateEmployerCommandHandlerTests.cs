using AutoMapper;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers;

public sealed class CreateEmployerCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    public CreateEmployerCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);

        _mapperMock
            .Setup(m => m.Map<EmployerDto>(It.IsAny<Employer>()))
            .Returns((Employer employer) => new EmployerDto(
                employer.Id,
                employer.UserId,
                employer.Name,
                employer.Email,
                employer.CompanyName,
                employer.Role,
                employer.IsVerified,
                employer.VerificationStatus,
                employer.CreatedAt,
                employer.UpdatedAt
            ));

        _tokenServiceMock
            .Setup(t => t.CreateAsync(It.IsAny<string>()))
            .ReturnsAsync(IdentityResponse<TokenResponse>.Ok(new TokenResponse
            {
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            }));

        _otpServiceMock
            .Setup(o => o.IsEmailVerifiedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_SuccessfullyCreatesEmployerAndMintsTokens_WhenValidCommandProvided()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "hr@acme.com",
            "Acme Corp",
            "P@ssword123!",
            "P@ssword123!"
        );

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        var identityResult = IdentityResponse<CreateUserResult>.Ok(
            new CreateUserResult { UserId = Guid.NewGuid().ToString() }
        );

        _identityServiceMock
            .Setup(i => i.CreateApplicationUserAsync(command.Email, command.Password, UserRole.Employer))
            .ReturnsAsync(identityResult);

        var handler = new CreateEmployerCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Employer);
        Assert.Equal(command.Email, result.Employer.Email);
        Assert.Equal(command.Name, result.Employer.Name);
        Assert.Equal(command.CompanyName, result.Employer.CompanyName);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);

        _employerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Employer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tokenServiceMock.Verify(t => t.CreateAsync(It.IsAny<string>()), Times.Once);
        _otpServiceMock.Verify(o => o.ConsumeEmailVerificationAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenEmployerAlreadyExistsAndIsVerified()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "verified@acme.com",
            "Acme Corp",
            "P@ssword123!",
            "P@ssword123!"
        );

        var existingEmployer = new Employer("user-123", "Jane Doe", "verified@acme.com", "Acme Corp");
        existingEmployer.MarkEmailVerified();

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployer);

        var handler = new CreateEmployerCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        _identityServiceMock.Verify(i => i.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenEmailIsNotVerifiedViaOtp()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "unverified-otp@acme.com",
            "Acme Corp",
            "P@ssword123!",
            "P@ssword123!"
        );

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        _otpServiceMock
            .Setup(o => o.IsEmailVerifiedAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateEmployerCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Email verification is required", exception.Message);
        _identityServiceMock.Verify(i => i.CreateApplicationUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatesAndMintsTokens_WhenEmployerExistsButIsNotVerified()
    {
        var command = new CreateEmployerCommand(
            "Jane Updated",
            "unverified@acme.com",
            "New Company Name",
            "P@ssword123!",
            "P@ssword123!"
        );

        var existingEmployer = new Employer("user-123", "Jane Doe", "unverified@acme.com", "Old Co");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployer);

        var handler = new CreateEmployerCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Employer);
        Assert.Equal("Jane Updated", existingEmployer.Name);
        Assert.Equal("New Company Name", existingEmployer.CompanyName);
        Assert.True(existingEmployer.IsVerified);
        Assert.Equal("test-access-token", result.AccessToken);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _otpServiceMock.Verify(o => o.ConsumeEmailVerificationAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenIdentityServiceFails()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "error@acme.com",
            "Acme Corp",
            "P@ssword123!",
            "P@ssword123!"
        );

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        var failedIdentityResult = IdentityResponse<CreateUserResult>.Fail("Identity creation error");

        _identityServiceMock
            .Setup(i => i.CreateApplicationUserAsync(command.Email, command.Password, UserRole.Employer))
            .ReturnsAsync(failedIdentityResult);

        var handler = new CreateEmployerCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Identity creation error", exception.Message);
    }
}
