using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Tests.Employers;

public sealed class VerifyEmployerEmailCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();

    public VerifyEmployerEmailCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_SuccessfullyMarksEmployerAsVerified_WhenOtpIsValid()
    {
        // Arrange
        var command = new VerifyEmployerEmailCommand("employer@acme.com", "123456");
        var employer = new Employer("user-1", "Jane Doe", "employer@acme.com", "Acme Corp");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(command.Email, command.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new VerifyEmployerEmailCommandHandler(_otpServiceMock.Object, _unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(employer.IsVerified);
        _employerRepositoryMock.Verify(r => r.Update(employer), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenEmployerDoesNotExist()
    {
        // Arrange
        var command = new VerifyEmployerEmailCommand("nonexistent@acme.com", "123456");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        var handler = new VerifyEmployerEmailCommandHandler(_otpServiceMock.Object, _unitOfWorkMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenOtpIsInvalidOrExpired()
    {
        // Arrange
        var command = new VerifyEmployerEmailCommand("employer@acme.com", "999999");
        var employer = new Employer("user-1", "Jane Doe", "employer@acme.com", "Acme Corp");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _otpServiceMock
            .Setup(o => o.VerifyOtpAsync(command.Email, command.Otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new VerifyEmployerEmailCommandHandler(_otpServiceMock.Object, _unitOfWorkMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Invalid or expired OTP code", ex.Message);
        Assert.False(employer.IsVerified);
    }
}
