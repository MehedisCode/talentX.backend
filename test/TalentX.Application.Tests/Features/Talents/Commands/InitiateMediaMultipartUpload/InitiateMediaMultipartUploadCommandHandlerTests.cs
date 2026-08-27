using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.InitiateMediaMultipartUpload;

public class InitiateMediaMultipartUploadCommandHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly InitiateMediaMultipartUploadCommandHandler _handler;

    public InitiateMediaMultipartUploadCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new InitiateMediaMultipartUploadCommandHandler(_multipartStorageService.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new InitiateMediaMultipartUploadCommand("video/mp4"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_BuildsStorageKeyUnderVideoIntrosPrefix_ScopedToUser()
    {
        string? capturedStorageKey = null;
        _multipartStorageService
            .Setup(s => s.InitiateMultipartUploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((storageKey, _, _) => capturedStorageKey = storageKey)
            .ReturnsAsync(new InitiateMultipartResult("upload-1", "video-intros/user-1/x.mp4", 5 * 1024 * 1024));

        await _handler.Handle(new InitiateMediaMultipartUploadCommand("video/mp4"), CancellationToken.None);

        capturedStorageKey.Should().StartWith("video-intros/user-1/");
        capturedStorageKey.Should().EndWith(".mp4");
    }

    [Fact]
    public async Task Handle_SanitizesUserIdSegment_BeforeItReachesTheStorageKey()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("../evil-user");
        string? capturedStorageKey = null;
        _multipartStorageService
            .Setup(s => s.InitiateMultipartUploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((storageKey, _, _) => capturedStorageKey = storageKey)
            .ReturnsAsync(new InitiateMultipartResult("upload-1", "video-intros/evil-user/x.mp4", 5 * 1024 * 1024));

        await _handler.Handle(new InitiateMediaMultipartUploadCommand("video/webm"), CancellationToken.None);

        capturedStorageKey.Should().NotContain("..");
    }
}
