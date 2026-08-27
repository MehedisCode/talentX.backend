using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.ListMediaMultipartParts;

public class ListMediaMultipartPartsQueryHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ListMediaMultipartPartsQueryHandler _handler;

    public ListMediaMultipartPartsQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");
        _handler = new ListMediaMultipartPartsQueryHandler(_multipartStorageService.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsTheStorageKey_ReturnsUploadedParts()
    {
        var expectedParts = new List<UploadedPartDto> { new(1, "\"etag-1\"", 1024) };
        _multipartStorageService
            .Setup(s => s.ListUploadedPartsAsync("video-intros/user-1/x.mp4", "upload-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedParts);

        var result = await _handler.Handle(
            new ListMediaMultipartPartsQuery("video-intros/user-1/x.mp4", "upload-1"),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expectedParts);
    }

    [Fact]
    public async Task Handle_WhenStorageKeyBelongsToAnotherUser_ThrowsForbiddenException()
    {
        var act = () => _handler.Handle(
            new ListMediaMultipartPartsQuery("video-intros/other-user/x.mp4", "upload-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenStorageKeyContainsDotDotSegment_ThrowsForbiddenException()
    {
        var act = () => _handler.Handle(
            new ListMediaMultipartPartsQuery("video-intros/user-1/../other-user/x.mp4", "upload-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
