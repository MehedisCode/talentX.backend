using FluentAssertions;
using MediatR;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Auth.ResetPassword;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.Users).Returns(_userRepositoryMock.Object);

        _handler = new ResetPasswordCommandHandler(
            _identityServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordSucceeds_ShouldReturnUnitValue()
    {
        var email = "user@example.com";
        var token = "valid-reset-token";
        var newPassword = "ValidPassword123!";

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _identityServiceMock.Verify(
            service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordSucceeds_ForGuestUser_ShouldPromoteToTalentAndMarkVerified()
    {
        var email = "guest@example.com";
        var token = "valid-reset-token";
        var newPassword = "ValidPassword123!";
        var domainUser = new User("user-guest-123", "Guest Candidate", email, UserRole.Guest, isVerified: false);

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(email))
            .ReturnsAsync(domainUser);

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        domainUser.Role.Should().Be(UserRole.Talent);
        domainUser.IsVerified.Should().BeTrue();

        _userRepositoryMock.Verify(repository => repository.Update(domainUser), Times.Once);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordSucceeds_ForEmployerUser_ShouldNotChangeRoleToTalent()
    {
        var email = "employer@example.com";
        var token = "valid-reset-token";
        var newPassword = "ValidPassword123!";
        var domainUser = new User("user-employer-123", "Employer Name", email, UserRole.Employer, isVerified: true);

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Ok());

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(email))
            .ReturnsAsync(domainUser);

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        domainUser.Role.Should().Be(UserRole.Employer);
        domainUser.IsVerified.Should().BeTrue();

        _userRepositoryMock.Verify(repository => repository.Update(domainUser), Times.Never);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordFails_ShouldThrowBadRequestException()
    {
        var email = "user@example.com";
        var token = "invalid-or-expired-token";
        var newPassword = "ValidPassword123!";

        _identityServiceMock
            .Setup(service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResponse.Fail("Invalid or expired password reset token."));

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        var handleAction = async () => await _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid or expired password reset token.");

        _identityServiceMock.Verify(
            service => service.ResetPasswordWithTokenAsync(
                email,
                token,
                newPassword,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(repository => repository.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }
}
