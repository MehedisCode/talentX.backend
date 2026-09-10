using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Tests.Features.SavedJobs.Commands.RemoveSavedJob;

public class RemoveSavedJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ISavedJobRepository> _savedJobRepository = new();
    private readonly RemoveSavedJobCommandHandler _handler;

    public RemoveSavedJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _unitOfWork.Setup(u => u.SavedJobs).Returns(_savedJobRepository.Object);
        _handler = new RemoveSavedJobCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_RemovesSavedJobSuccessfully()
    {
        var jobId = Guid.NewGuid();
        var savedJob = SavedJob.Create(jobId, "talent-1");
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedJob);

        await _handler.Handle(new RemoveSavedJobCommand(jobId), CancellationToken.None);

        _savedJobRepository.Verify(r => r.Remove(savedJob), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSavedJobNotFound_ThrowsNotFoundException()
    {
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedJob?)null);

        var act = () => _handler.Handle(new RemoveSavedJobCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new RemoveSavedJobCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenSavedJobBelongsToAnotherTalent_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedJob?)null);

        var act = () => _handler.Handle(new RemoveSavedJobCommand(jobId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CallsSaveChangesExactlyOnce()
    {
        var jobId = Guid.NewGuid();
        var savedJob = SavedJob.Create(jobId, "talent-1");
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedJob);

        await _handler.Handle(new RemoveSavedJobCommand(jobId), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
