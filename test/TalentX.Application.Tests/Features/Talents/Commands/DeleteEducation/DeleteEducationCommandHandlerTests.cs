using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.DeleteEducation;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteEducation;

public class DeleteEducationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeleteEducationCommandHandler _handler;

    public DeleteEducationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new DeleteEducationCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenOwnedByCallingUser_DeletesAndSaves()
    {
        var education = new Education("user-1", "State University", "BSc");
        _educationRepository.Setup(r => r.GetByIdAsync(education.Id, It.IsAny<CancellationToken>())).ReturnsAsync(education);

        await _handler.Handle(new DeleteEducationCommand(education.Id), CancellationToken.None);

        _educationRepository.Verify(r => r.Delete(education), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherUser_ThrowsNotFoundException_NotSilentNoOp()
    {
        var education = new Education("someone-else", "State University", "BSc");
        _educationRepository.Setup(r => r.GetByIdAsync(education.Id, It.IsAny<CancellationToken>())).ReturnsAsync(education);

        var act = () => _handler.Handle(new DeleteEducationCommand(education.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _educationRepository.Verify(r => r.Delete(It.IsAny<Education>()), Times.Never);
    }
}
