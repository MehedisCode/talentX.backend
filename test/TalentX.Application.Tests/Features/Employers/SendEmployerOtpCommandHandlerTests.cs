using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.Commands.SendOtp;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers;

public sealed class SendEmployerOtpCommandHandlerTests
{
    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmployerRepository> _employerRepositoryMock = new();

    public SendEmployerOtpCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Employers).Returns(_employerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_SuccessfullySendsNewOtp_WhenEmployerIsUnverified()
    {
        var command = new SendEmployerOtpCommand("unverified@acme.com");
        var employer = new Employer("user-1", "Jane Doe", "unverified@acme.com", "Acme Corp");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        _otpServiceMock
            .Setup(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("789012");

        var handler = new SendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _otpServiceMock.Verify(o => o.ConsumeEmailVerificationAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendOtpEmailAsync(command.Email, "Jane Doe", "789012", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfullySendsOtp_WhenEmployerDoesNotExistYet()
    {
        var command = new SendEmployerOtpCommand("newuser@acme.com");

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employer?)null);

        _otpServiceMock
            .Setup(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        var handler = new SendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _otpServiceMock.Verify(o => o.ConsumeEmailVerificationAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(command.Email, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendOtpEmailAsync(command.Email, "Employer", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsConflictException_WhenEmployerAlreadyVerified()
    {
        var command = new SendEmployerOtpCommand("verified@acme.com");
        var employer = new Employer("user-1", "Jane Doe", "verified@acme.com", "Acme Corp");
        employer.MarkAsVerified();

        _employerRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var handler = new SendEmployerOtpCommandHandler(
            _otpServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object
        );

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already registered and verified", exception.Message);
        _otpServiceMock.Verify(o => o.GenerateOtpAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
