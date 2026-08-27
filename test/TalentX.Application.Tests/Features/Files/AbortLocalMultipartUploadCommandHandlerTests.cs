using FluentAssertions;
using MediatR;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Files;

public sealed class AbortLocalMultipartUploadCommandHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageServiceMock;
    private readonly AbortLocalMultipartUploadCommandHandler _handler;

    public AbortLocalMultipartUploadCommandHandlerTests()
    {
        _multipartStorageServiceMock = new Mock<IMultipartStorageService>();
        _handler = new AbortLocalMultipartUploadCommandHandler(_multipartStorageServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToMultipartStorageService()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-abort-123";

        var command = new AbortLocalMultipartUploadCommand(storageKey, uploadId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _multipartStorageServiceMock.Verify(service =>
            service.AbortMultipartUploadAsync(storageKey, uploadId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
