using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.AbortMediaMultipartUpload;

public class AbortMediaMultipartUploadCommandHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly AbortMediaMultipartUploadCommandHandler _handler;

    public AbortMediaMultipartUploadCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new AbortMediaMultipartUploadCommandHandler(_multipartStorageService.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsTheStorageKey_AbortsTheUpload()
    {
        await _handler.Handle(new AbortMediaMultipartUploadCommand("video-intros/user-1/x.mp4", "upload-1"), CancellationToken.None);

        _multipartStorageService.Verify(
            s => s.AbortMultipartUploadAsync("video-intros/user-1/x.mp4", "upload-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStorageKeyBelongsToAnotherUser_ThrowsForbiddenException_AndNeverAborts()
    {
        var act = () => _handler.Handle(new AbortMediaMultipartUploadCommand("video-intros/other-user/x.mp4", "upload-1"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _multipartStorageService.Verify(
            s => s.AbortMultipartUploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStorageKeyContainsDotDotSegment_ThrowsForbiddenException_AndNeverAborts()
    {
        var act = () => _handler.Handle(
            new AbortMediaMultipartUploadCommand("video-intros/user-1/../other-user/x.mp4", "upload-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _multipartStorageService.Verify(
            s => s.AbortMultipartUploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
