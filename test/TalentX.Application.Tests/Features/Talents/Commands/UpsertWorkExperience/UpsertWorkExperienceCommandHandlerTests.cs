using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertWorkExperience;

public class UpsertWorkExperienceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWorkExperienceRepository> _workExperienceRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpsertWorkExperienceCommandHandler _handler;

    public UpsertWorkExperienceCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.WorkExperiences).Returns(_workExperienceRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new UpsertWorkExperienceCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenIdIsNull_CreatesNewWorkExperience()
    {
        var command = new UpsertWorkExperienceCommand(null, "Acme", "Engineer", "2020", null, true);

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
        _workExperienceRepository.Verify(r => r.AddAsync(It.IsAny<WorkExperience>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToCallingUser_UpdatesExistingRow()
    {
        var existing = new WorkExperience("user-1", "Old Co", "Old Title");
        var command = new UpsertWorkExperienceCommand(existing.Id, "Acme", "Engineer", "2020", "Present", true);
        _workExperienceRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _handler.Handle(command, CancellationToken.None);

        existing.CompanyName.Should().Be("Acme");
        existing.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenIdBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var existing = new WorkExperience("someone-else", "Old Co", "Old Title");
        var command = new UpsertWorkExperienceCommand(existing.Id, "Acme", "Engineer", "2020", "Present", true);
        _workExperienceRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenIdDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        var command = new UpsertWorkExperienceCommand(missingId, "Acme", "Engineer", "2020", "Present", true);
        _workExperienceRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((WorkExperience?)null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
