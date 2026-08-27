using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using TalentX.API.Controllers.Files;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;
using TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;
using TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;
using TalentX.Application.Features.Files.Commands.UploadLocalFile;
using TalentX.Application.Features.Files.Queries.GetLocalFileStream;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Files;

public sealed class LocalFilesControllerTests
{
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
    private readonly Mock<ISender> _senderMock;
    private readonly LocalFilesController _controller;

    public LocalFilesControllerTests()
    {
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock.Setup(environment => environment.EnvironmentName).Returns(Environments.Development);

        _senderMock = new Mock<ISender>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_senderMock.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        _controller = new LocalFilesController(_hostEnvironmentMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    [Fact]
    public async Task Stream_SendsGetLocalFileStreamQuery_AndReturnsFileResult()
    {
        var storageKey = "resumes/candidate.pdf";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("File content"));
        var queryResult = new LocalFileStreamResult(memoryStream, "application/pdf", "candidate.pdf");

        _senderMock
            .Setup(sender => sender.Send(It.Is<GetLocalFileStreamQuery>(query => query.StorageKey == storageKey), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        var result = await _controller.Stream(storageKey, CancellationToken.None);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.EnableRangeProcessing.Should().BeTrue();
        fileResult.FileStream.Should().BeSameAs(memoryStream);
    }

    [Fact]
    public async Task Stream_ReturnsNotFound_WhenOutsideDevelopment()
    {
        _hostEnvironmentMock.Setup(environment => environment.EnvironmentName).Returns(Environments.Production);

        var result = await _controller.Stream("sample.pdf", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Upload_SendsUploadLocalFileCommand_AndReturnsOkWithStorageKey()
    {
        var storageKey = "resumes/candidate.pdf";
        var payloadBytes = Encoding.UTF8.GetBytes("Resume payload");
        _controller.HttpContext.Request.Body = new MemoryStream(payloadBytes);

        _senderMock
            .Setup(sender => sender.Send(It.Is<UploadLocalFileCommand>(command => command.StorageKey == storageKey), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);

        var result = await _controller.Upload(storageKey, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { StorageKey = storageKey });
    }

    [Fact]
    public async Task Upload_ReturnsNotFound_WhenOutsideDevelopment()
    {
        _hostEnvironmentMock.Setup(environment => environment.EnvironmentName).Returns(Environments.Production);

        var result = await _controller.Upload("sample.pdf", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UploadPart_SendsSaveLocalMultipartPartCommand_AndReturnsOkWithUploadedPart()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-session-123";
        var partNumber = 1;
        var uploadedPart = new UploadedPartDto(partNumber, "\"part-1\"", 1024);

        _controller.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Chunk data"));

        _senderMock
            .Setup(sender => sender.Send(It.Is<SaveLocalMultipartPartCommand>(command =>
                command.StorageKey == storageKey &&
                command.UploadId == uploadId &&
                command.PartNumber == partNumber), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedPart);

        var result = await _controller.UploadPart(storageKey, uploadId, partNumber, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(uploadedPart);
    }

    [Fact]
    public async Task CompleteMultipart_SendsCompleteLocalMultipartUploadCommand_AndReturnsOk()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-session-123";
        var parts = new List<PartETagDto>
        {
            new(1, "\"part-1\""),
            new(2, "\"part-2\"")
        };

        _senderMock
            .Setup(sender => sender.Send(It.Is<CompleteLocalMultipartUploadCommand>(command =>
                command.StorageKey == storageKey &&
                command.UploadId == uploadId &&
                command.Parts == parts), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.CompleteMultipart(storageKey, uploadId, parts, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new
        {
            StorageKey = storageKey,
            UploadId = uploadId,
            Status = "Completed"
        });
    }

    [Fact]
    public async Task AbortMultipart_SendsAbortLocalMultipartUploadCommand_AndReturnsNoContent()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-session-123";

        _senderMock
            .Setup(sender => sender.Send(It.Is<AbortLocalMultipartUploadCommand>(command =>
                command.StorageKey == storageKey &&
                command.UploadId == uploadId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.AbortMultipart(storageKey, uploadId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
