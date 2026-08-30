using FluentAssertions;
using MediatR;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Files;

public sealed class CompleteLocalMultipartUploadCommandHandlerTests
{
    private readonly Mock<IMultipartStorageService> _multipartStorageServiceMock;
    private readonly CompleteLocalMultipartUploadCommandHandler _handler;

    public CompleteLocalMultipartUploadCommandHandlerTests()
    {
        _multipartStorageServiceMock = new Mock<IMultipartStorageService>();
        _handler = new CompleteLocalMultipartUploadCommandHandler(_multipartStorageServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToMultipartStorageService()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-123";
        var parts = new List<PartETagDto>
        {
            new(1, "\"part-1\"")
        };

        var command = new CompleteLocalMultipartUploadCommand(storageKey, uploadId, parts);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _multipartStorageServiceMock.Verify(service =>
            service.CompleteMultipartUploadAsync(storageKey, uploadId, parts, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
