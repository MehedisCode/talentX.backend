using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Commands.UpdateUserAccountStatus;

public sealed class UpdateUserAccountStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateUserAccountStatusCommandHandler _handler;

    public UpdateUserAccountStatusCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(u => u.AuditLogs).Returns(_auditLogRepository.Object);
        _currentUserService.SetupGet(s => s.Email).Returns("admin@talentx.com");

        _handler = new UpdateUserAccountStatusCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoEmail_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(s => s.Email).Returns((string?)null);

        var command = new UpdateUserAccountStatusCommand(Guid.NewGuid(), AccountStatus.Deactivated);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var command = new UpdateUserAccountStatusCommand(userId, AccountStatus.Deactivated);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetIsAdmin_ThrowsForbiddenException()
    {
        var admin = new User("admin-id", "Admin", "admin2@talentx.com", UserRole.Admin, isVerified: true);
        _userRepository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var command = new UpdateUserAccountStatusCommand(admin.Id, AccountStatus.Deactivated);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.Talent)]
    [InlineData(UserRole.Employer)]
    [InlineData(UserRole.Guest)]
    public async Task Handle_WhenDeactivatingActiveAccount_DeactivatesAndWritesAuditLog(UserRole role)
    {
        var user = new User("user-id", "Jordan Smith", "jordan@talentx.com", role, isVerified: true);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateUserAccountStatusCommand(user.Id, AccountStatus.Deactivated);
        var result = await _handler.Handle(command, CancellationToken.None);

        user.Status.Should().Be(AccountStatus.Deactivated);
        result.Message.Should().Contain("Deactivated");
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _auditLogRepository.Verify(
            r => r.AddAsync(
                It.Is<AuditLog>(log => log.Actor == "admin@talentx.com" && log.Action.Contains("Deactivated account jordan@talentx.com")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeactivatingAlreadyDeactivatedAccount_IsIdempotentAndSucceeds()
    {
        var user = new User("user-id", "Jordan Smith", "jordan@talentx.com", UserRole.Talent, isVerified: true);
        user.Deactivate();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateUserAccountStatusCommand(user.Id, AccountStatus.Deactivated);
        var result = await _handler.Handle(command, CancellationToken.None);

        user.Status.Should().Be(AccountStatus.Deactivated);
        result.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenActivatingAlreadyActiveAccount_IsIdempotentAndSucceeds()
    {
        var user = new User("user-id", "Jordan Smith", "jordan@talentx.com", UserRole.Employer, isVerified: true);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateUserAccountStatusCommand(user.Id, AccountStatus.Active);
        var result = await _handler.Handle(command, CancellationToken.None);

        user.Status.Should().Be(AccountStatus.Active);
        result.Message.Should().Contain("Active");
        _auditLogRepository.Verify(
            r => r.AddAsync(
                It.Is<AuditLog>(log => log.Action.Contains("Activated account jordan@talentx.com")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
