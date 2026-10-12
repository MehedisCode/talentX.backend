using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.UpsertProject;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertProject;

public class UpsertProjectCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpsertProjectCommandHandler _handler;

    public UpsertProjectCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Projects).Returns(_projectRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new UpsertProjectCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenIdIsNull_CreatesNewProject()
    {
        var command = new UpsertProjectCommand
        {
            ProjectName = "TalentX Platform",
            Role = "Tech Lead",
            ProjectType = "Professional",
            StartDate = "2023",
            EndDate = "Present",
            IsOngoing = true,
            Description = "AI talent matchmaking platform",
            TechnologiesUsed = ["C#", ".NET", "PostgreSQL"],
            ProjectUrl = "https://talentx.ai"
        };

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        _projectRepository.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToCallingUser_UpdatesExistingRow()
    {
        var existing = new Project(
            "user-1", "Old Project", "Dev", "Personal", "2022", null, false, "Old desc", [], null);
        var command = new UpsertProjectCommand
        {
            Id = existing.Id,
            ProjectName = "Updated Project",
            Role = "Lead",
            ProjectType = "Professional",
            StartDate = "2023",
            EndDate = "Present",
            IsOngoing = true,
            Description = "Updated desc",
            TechnologiesUsed = ["React"],
            ProjectUrl = "https://example.com"
        };
        _projectRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().Be(existing.Id);
        existing.ProjectName.Should().Be("Updated Project");
        existing.Role.Should().Be("Lead");
        existing.ProjectType.Should().Be("Professional");
        existing.IsOngoing.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var existing = new Project(
            "someone-else", "Foreign Project", "Dev", "Personal", "2022", null, false, "Foreign desc", [], null);
        var command = new UpsertProjectCommand
        {
            Id = existing.Id,
            ProjectName = "Hacked Project",
            Role = "Lead",
            ProjectType = "Professional",
            StartDate = "2023",
            Description = "Hacked desc"
        };
        _projectRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenIdDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        var command = new UpsertProjectCommand
        {
            Id = missingId,
            ProjectName = "Missing Project",
            Role = "Dev",
            ProjectType = "Professional",
            StartDate = "2023",
            Description = "Missing desc"
        };
        _projectRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var command = new UpsertProjectCommand
        {
            ProjectName = "Unauth Project",
            Role = "Dev",
            ProjectType = "Professional",
            StartDate = "2023",
            Description = "Unauth desc"
        };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
