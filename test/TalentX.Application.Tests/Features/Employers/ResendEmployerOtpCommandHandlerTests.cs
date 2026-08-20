using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.Commands.ResendOtp;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers;

public sealed class ResendEmployerOtpCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();

    public ResendEmployerOtpCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_SuccessfullySendsNewOtp_WhenEmployerIsUnverified()
    {
        // Arrange
        var command = new ResendEmployerOtpCommand("unverified@acme.com");
        var employer = new Employer("user-1", "Jane Doe", "unverified@acme.com", "Acme Corp");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _otpServiceMock
            .Setup(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("789012");

        var handler = new ResendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendOtpEmailAsync(command.Email, "Jane Doe", "789012", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenEmployerNotFound()
    {
        // Arrange
        var command = new ResendEmployerOtpCommand("missing@acme.com");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        var handler = new ResendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenEmployerAlreadyVerified()
    {
        // Arrange
        var command = new ResendEmployerOtpCommand("verified@acme.com");
        var employer = new Employer("user-1", "Jane Doe", "verified@acme.com", "Acme Corp");
        employer.MarkAsVerified();

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var handler = new ResendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already verified", ex.Message);
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
