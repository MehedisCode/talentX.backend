using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.RequestMediaMultipartPartUrl;

public class RequestMediaMultipartPartUrlCommandHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly RequestMediaMultipartPartUrlCommandHandler _handler;

    public RequestMediaMultipartPartUrlCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new RequestMediaMultipartPartUrlCommandHandler(_multipartStorageService.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsTheStorageKey_ReturnsPresignedUrl()
    {
        _multipartStorageService
            .Setup(s => s.CreatePartUploadUrlAsync("video-intros/user-1/x.mp4", "upload-1", 1, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/part-1");

        var result = await _handler.Handle(
            new RequestMediaMultipartPartUrlCommand("video-intros/user-1/x.mp4", "upload-1", 1),
            CancellationToken.None);

        result.Should().Be("https://example.com/part-1");
    }

    [Fact]
    public async Task Handle_WhenStorageKeyBelongsToAnotherUser_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(
            new RequestMediaMultipartPartUrlCommand("video-intros/other-user/x.mp4", "upload-1", 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenStorageKeyContainsDotDotSegment_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(
            new RequestMediaMultipartPartUrlCommand("video-intros/user-1/../other-user/x.mp4", "upload-1", 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(
            new RequestMediaMultipartPartUrlCommand("video-intros/user-1/x.mp4", "upload-1", 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
