using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Resume;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Resumes.Commands.UploadResume;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Resume;

public class ResumesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly ResumesController _controller;

    public ResumesControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new ResumesController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public async Task Upload_SendsUploadResumeCommand_AndReturnsOkResultWithExtractionResult()
    {
        // Arrange
        var pdfHeader = "%PDF-1.4 header bytes"u8.ToArray();
        var stream = new MemoryStream(pdfHeader);

        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("candidate_resume.pdf");
        formFileMock.Setup(f => f.ContentType).Returns("application/pdf");
        formFileMock.Setup(f => f.Length).Returns(pdfHeader.Length);
        formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var expectedResult = new ResumeExtractionResult
        {
            ResumeUploadId = Guid.NewGuid(),
            FullName = "Jane Developer",
            Email = "jane.dev@example.com",
            Phone = "+1987654321",
            FileStorageKey = "resumes/jane_resume.pdf"
        };

        _sender
            .Setup(s => s.Send(It.IsAny<UploadResumeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.Upload(formFileMock.Object, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        _sender.Verify(s => s.Send(It.IsAny<UploadResumeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
