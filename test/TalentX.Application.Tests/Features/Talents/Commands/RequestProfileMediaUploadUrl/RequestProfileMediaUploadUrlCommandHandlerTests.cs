using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.RequestProfileMediaUploadUrl;

public class RequestProfileMediaUploadUrlCommandHandlerTests
{
    private readonly Mock<IPresignedUrlService> _presignedUrlService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly RequestProfileMediaUploadUrlCommandHandler _handler;

    public RequestProfileMediaUploadUrlCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new RequestProfileMediaUploadUrlCommandHandler(_presignedUrlService.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new RequestProfileMediaUploadUrlCommand("image/jpeg"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_BuildsStorageKeyUnderProfilePicturesPrefix_ScopedToUser()
    {
        string? capturedStorageKey = null;
        _presignedUrlService
            .Setup(s => s.CreateUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TimeSpan, CancellationToken>((storageKey, _, _, _) => capturedStorageKey = storageKey)
            .ReturnsAsync(new PresignedUploadTarget("https://example.com/upload", "profile-pictures/user-1/x.jpg", DateTimeOffset.UtcNow));

        await _handler.Handle(new RequestProfileMediaUploadUrlCommand("image/jpeg"), CancellationToken.None);

        capturedStorageKey.Should().StartWith("profile-pictures/user-1/");
        capturedStorageKey.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task Handle_WhenUserIdContainsSlash_ThrowsBadRequestException_AndNeverRequestsUploadUrl()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("../evil-user");

        var act = () => _handler.Handle(new RequestProfileMediaUploadUrlCommand("image/png"), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _presignedUrlService.Verify(
            s => s.CreateUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
