using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteWorkExperience;

public class DeleteWorkExperienceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeleteWorkExperienceCommandHandler _handler;

    public DeleteWorkExperienceCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new DeleteWorkExperienceCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenOwnedByCallingUser_DeletesAndSaves()
    {
        var workExperience = new WorkExperience("user-1", "Acme", "Engineer");
        _workExperienceRepository.Setup(r => r.GetByIdAsync(workExperience.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workExperience);

        await _handler.Handle(new DeleteWorkExperienceCommand(workExperience.Id), CancellationToken.None);

        _workExperienceRepository.Verify(r => r.Delete(workExperience), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherUser_ThrowsNotFoundException_NotSilentNoOp()
    {
        var workExperience = new WorkExperience("someone-else", "Acme", "Engineer");
        _workExperienceRepository.Setup(r => r.GetByIdAsync(workExperience.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workExperience);

        var act = () => _handler.Handle(new DeleteWorkExperienceCommand(workExperience.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _workExperienceRepository.Verify(r => r.Delete(It.IsAny<WorkExperience>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRowDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _workExperienceRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkExperience?)null);

        var act = () => _handler.Handle(new DeleteWorkExperienceCommand(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
