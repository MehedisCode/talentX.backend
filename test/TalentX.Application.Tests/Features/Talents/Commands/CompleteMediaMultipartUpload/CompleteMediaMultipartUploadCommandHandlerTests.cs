using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.CompleteMediaMultipartUpload;

public class CompleteMediaMultipartUploadCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talentRepository = new();
    private readonly Mock<IMultipartStorageService> _multipartStorageService = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly CompleteMediaMultipartUploadCommandHandler _handler;

    public CompleteMediaMultipartUploadCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talentRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new CompleteMediaMultipartUploadCommandHandler(
            _unitOfWork.Object,
            _multipartStorageService.Object,
            _fileStorageService.Object,
            _currentUserService.Object);
    }

    private static CompleteMediaMultipartUploadCommand ValidCommand(string storageKey = "video-intros/user-1/new.mp4") => new(
        storageKey,
        "upload-1",
        [new CompleteMediaMultipartUploadPartItem(1, "\"etag-1\"")]
    );

    [Fact]
    public async Task Handle_WhenStorageKeyBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(ValidCommand("video-intros/other-user/new.mp4"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenStorageKeyContainsDotDotSegment_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(ValidCommand("video-intros/user-1/../other-user/new.mp4"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNoPreviousVideo_SetsKey_AndNeverDeletes()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        talent.VideoIntroStorageKey.Should().Be("video-intros/user-1/new.mp4");
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPreviousVideoDiffersFromNew_DeletesSupersededObject()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        talent.UpdateVideoIntro("video-intros/user-1/old.mp4");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _fileStorageService.Verify(s => s.DeleteAsync("video-intros/user-1/old.mp4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsCompleteMultipartUploadWithMappedParts()
    {
        var talent = new Talent("user-1", "Jane Doe", "jane@example.com");
        _talentRepository.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(talent);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _multipartStorageService.Verify(
            s => s.CompleteMultipartUploadAsync(
                "video-intros/user-1/new.mp4",
                "upload-1",
                It.Is<IReadOnlyList<PartETagDto>>(parts => parts.Count == 1 && parts[0].PartNumber == 1 && parts[0].ETag == "\"etag-1\""),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
