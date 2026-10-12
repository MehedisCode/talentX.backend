using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.DeleteProject;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteProject;

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Projects).Returns(_projectRepository.Object);
        _currentUserService.SetupGet(userService => userService.UserId).Returns("user-1");
        _handler = new DeleteProjectCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenOwnedByCallingUser_DeletesAndSaves()
    {
        var project = new Project(
            "user-1", "My Project", "Role", "Professional", "2023", null, true, "Desc", [], null);
        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        _projectRepository.Verify(repository => repository.Delete(project), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var project = new Project(
            "someone-else", "Other Project", "Role", "Professional", "2023", null, true, "Desc", [], null);
        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = () => _handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _projectRepository.Verify(repository => repository.Delete(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRowDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _projectRepository.Setup(repository => repository.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var act = () => _handler.Handle(new DeleteProjectCommand(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(userService => userService.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
