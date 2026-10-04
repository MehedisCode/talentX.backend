using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Resumes.Commands.DeleteResume;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class DeleteResumeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IResumeRepository> _resumeRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<DeleteResumeCommandHandler>> _logger = new();
    private readonly DeleteResumeCommandHandler _handler;

    public DeleteResumeCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Resumes).Returns(_resumeRepository.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new DeleteResumeCommandHandler(
            _unitOfWork.Object,
            _fileStorageService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_DeletesUnusedResumeRowAndStoredFile()
    {
        var resume = new Resume("user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume> { resume });
        _jobApplicationRepository
            .Setup(r => r.CountByResumeStorageKeyAsync("resumes/user-1/a.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _handler.Handle(new DeleteResumeCommand(resume.Id), CancellationToken.None);

        _resumeRepository.Verify(r => r.Delete(resume), Times.Once);
        _fileStorageService.Verify(
            s => s.DeleteAsync("resumes/user-1/a.pdf", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResumeIsUsedByApplications_ThrowsConflict_AndDeletesNothing()
    {
        var resume = new Resume("user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024);
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume> { resume });
        _jobApplicationRepository
            .Setup(r => r.CountByResumeStorageKeyAsync("resumes/user-1/a.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var act = () => _handler.Handle(new DeleteResumeCommand(resume.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _resumeRepository.Verify(r => r.Delete(It.IsAny<Resume>()), Times.Never);
        _fileStorageService.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeletingTheDefault_PromotesTheNewestRemainingResume()
    {
        var defaultResume = new Resume(
            "user-1", "resumes/user-1/a.pdf", "a.pdf", "application/pdf", 1024, isDefault: true);
        var older = new Resume("user-1", "resumes/user-1/b.pdf", "b.pdf", "application/pdf", 1024);
        var newer = new Resume("user-1", "resumes/user-1/c.pdf", "c.pdf", "application/pdf", 1024);
        older.UpdateFile("resumes/user-1/b.pdf", "b.pdf", "application/pdf", 1024);
        newer.UpdateFile("resumes/user-1/c.pdf", "c.pdf", "application/pdf", 1024);

        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume> { defaultResume, newer, older });

        await _handler.Handle(new DeleteResumeCommand(defaultResume.Id), CancellationToken.None);

        newer.IsDefault.Should().BeTrue();
        older.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenResumeNotFound_ThrowsNotFoundException()
    {
        _resumeRepository
            .Setup(r => r.GetAllByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Resume>());

        var act = () => _handler.Handle(new DeleteResumeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
