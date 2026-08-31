using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ConfirmProfileMediaUpload;

public class ConfirmProfileMediaUploadCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ConfirmProfileMediaUploadCommandHandler _handler;

    public ConfirmProfileMediaUploadCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new ConfirmProfileMediaUploadCommandHandler(
            _unitOfWork.Object,
            _fileStorageService.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenStorageKeyBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new ConfirmProfileMediaUploadCommand("profile-pictures/other-user/new.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenStorageKeyContainsDotDotSegment_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(
            new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/../other-user/new.jpg"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTalentDoesNotExist_ThrowsNotFoundException()
    {
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((Talent?)null);

        var act = () => _handler.Handle(new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/new.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNoPreviousPicture_UpdatesKey_AndNeverDeletes()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/new.jpg"), CancellationToken.None);

        talent.ProfilePictureStorageKey.Should().Be("profile-pictures/user-1/new.jpg");
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPreviousPictureDiffersFromNew_DeletesSupersededObject()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        talent.UpdateProfilePicture("profile-pictures/user-1/old.jpg");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/new.jpg"), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync("profile-pictures/user-1/old.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewKeyEqualsOldKey_NeverDeletes()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        talent.UpdateProfilePicture("profile-pictures/user-1/same.jpg");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/same.jpg"), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
