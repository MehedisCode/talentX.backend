using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Resumes.Commands.RenameResume;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class RenameResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly RenameResumeCommandHandler _handler;

    public RenameResumeCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RenameResumeCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_RenamesOwnedResume()
    {
        var resume = new Resume("user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var result = await _handler.Handle(
            new RenameResumeCommand(resume.Id, "New Name"),
            CancellationToken.None);

        resume.DisplayName.Should().Be("New Name");
        result.DisplayName.Should().Be("New Name");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResumeBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var resume = new Resume("other-user", "resumes/other-user/a.pdf", "a.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetByIdAsync(resume.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resume);

        var act = () => _handler.Handle(
            new RenameResumeCommand(resume.Id, "New Name"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResumeDoesNotExist_ThrowsNotFoundException()
    {
        _resumeRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume?)null);

        var act = () => _handler.Handle(
            new RenameResumeCommand(Guid.NewGuid(), "New Name"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
