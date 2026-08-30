using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.UpsertEducation;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertEducation;

public class UpsertEducationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEducationRepository> _educationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpsertEducationCommandHandler _handler;

    public UpsertEducationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Educations).Returns(_educationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new UpsertEducationCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenIdIsNull_CreatesNewEducation()
    {
        var command = new UpsertEducationCommand(null, "State University", "BSc", "2016", "2020");

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        _educationRepository.Verify(r => r.AddAsync(It.IsAny<Education>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToCallingUser_UpdatesExistingRow()
    {
        var existing = new Education("user-1", "Old School", "Old Degree");
        var command = new UpsertEducationCommand(existing.Id, "State University", "BSc", "2016", "2020");
        _educationRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _handler.Handle(command, CancellationToken.None);

        existing.Institution.Should().Be("State University");
        _educationRepository.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var existing = new Education("someone-else", "Old School", "Old Degree");
        var command = new UpsertEducationCommand(existing.Id, "State University", "BSc", "2016", "2020");
        _educationRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
