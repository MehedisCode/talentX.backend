using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Resumes.Commands.SetDefaultResume;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class SetDefaultResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly SetDefaultResumeCommandHandler _handler;

    public SetDefaultResumeCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new SetDefaultResumeCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_SetsTargetDefaultAndClearsPreviousDefault()
    {
        var previous = new Resume("user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024);
        previous.SetDefault();
        var target = new Resume("user-1", "resumes/user-1/b.pdf", "b.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume> { previous, target });

        var result = await _handler.Handle(
            new SetDefaultResumeCommand(target.Id),
            CancellationToken.None);

        previous.IsDefault.Should().BeFalse();
        target.IsDefault.Should().BeTrue();
        result.Id.Should().Be(target.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResumeNotOwned_ThrowsNotFoundException()
    {
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume>());

        var act = () => _handler.Handle(
            new SetDefaultResumeCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
