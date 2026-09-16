using AutoMapper;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers;

public sealed class VerifyEmployerEmailCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    public VerifyEmployerEmailCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditLogs).Returns(_auditLogRepositoryMock.Object);

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
                AccessToken = "mock-access-token",
                RefreshToken = "mock-refresh-token"
            }));
    }

    [Fact]
    public async Task Handle_SuccessfullyMarksEmployerAsVerified_AndReturnsTokens_WhenOtpIsValid()
    {
        var command = new VerifyEmployerEmailCommand("employer@acme.com", "123456");
        var employer = new Employer("user-1", "Jane Doe", "employer@acme.com", "Acme Corp");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(command.Email, command.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new VerifyEmployerEmailCommandHandler(
            _otpServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Employer);
        Assert.True(employer.IsVerified);
        Assert.Equal("employer@acme.com", result.Employer.Email);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.Equal("mock-refresh-token", result.RefreshToken);

        _otpServiceMock.Verify(o => o.MarkEmailVerifiedAsync(command.Email, null, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tokenServiceMock.Verify(t => t.CreateAsync("user-1"), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyTokens_WhenEmployerDoesNotExistPriorToOnboarding_AndOtpIsValid()
    {
        var command = new VerifyEmployerEmailCommand("newuser@acme.com", "123456");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(command.Email, command.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new VerifyEmployerEmailCommandHandler(
            _otpServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Employer);
        Assert.Equal(string.Empty, result.AccessToken);

        _otpServiceMock.Verify(o => o.MarkEmailVerifiedAsync(command.Email, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenOtpIsInvalidOrExpired()
    {
        var command = new VerifyEmployerEmailCommand("employer@acme.com", "999999");

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(command.Email, command.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new VerifyEmployerEmailCommandHandler(
            _otpServiceMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Invalid or expired OTP code", exception.Message);
        _otpServiceMock.Verify(o => o.MarkEmailVerifiedAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
