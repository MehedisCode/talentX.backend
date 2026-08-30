using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Talents;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;
using TalentX.Application.Features.Talents.Commands.DeleteEducation;
using TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;
using TalentX.Application.Features.Talents.Commands.ExtractResume;
using TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;
using TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Features.Talents.Commands.UpsertEducation;
using TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Talents;

public class TalentsControllerTests
{
    private readonly Mock<ISender> _sender = new();

    private TalentsController CreateController()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        return new TalentsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_WithTalentRole()
    {
        var attribute = typeof(TalentsController)
            .GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be(nameof(UserRole.Talent));
    }

    [Fact]
    public async Task GetMyProfile_SendsQuery_AndReturnsOk()
    {
        var expectedResponse = new TalentProfileResponse(
            Guid.NewGuid(), "user-1", "Jane Doe", "jane@example.com",
            null, null, null, null, null, new List<string>(),
            null, null, new List<string>(), new List<string>(), new List<string>(), new List<string>(),
            null, null, null, null,
            new List<WorkExperienceItemDto>(), new List<EducationItemDto>(),
            DateTime.UtcNow, null);

        _sender
            .Setup(s => s.Send(It.IsAny<GetTalentProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = CreateController();

        var actionResult = await controller.GetMyProfile(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        _sender.Verify(
            s => s.Send(It.IsAny<GetTalentProfileQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMyProfile_SendsUpdateTalentProfileCommand_AndReturnsNoContent()
    {
        var command = new UpdateTalentProfileCommand(
            "Jane Doe", "Dhaka", "Backend Engineer", "Bio", "+8801000000000",
            "https://linkedin.com/in/janedoe", null, 5m, 120000m, null);

        var controller = CreateController();

        var actionResult = await controller.UpdateMyProfile(command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyApplicationPrefill_SendsQuery_AndReturnsOk()
    {
        var expectedResponse = new TalentApplicationPrefillResponse(
            "Jane Doe", "jane@example.com", null, null, null, null, null, null, null,
            new List<WorkExperienceItemDto>());

        _sender
            .Setup(s => s.Send(It.IsAny<GetTalentApplicationPrefillQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = CreateController();

        var actionResult = await controller.GetMyApplicationPrefill(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ExtractResume_SendsExtractResumeCommand_AndReturnsOkWithExtractionPreview()
    {
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("resume.pdf");
        formFileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var expectedResult = new ResumeExtractionResult { FullName = "Jane Doe" };
        _sender
            .Setup(s => s.Send(It.IsAny<ExtractResumeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var controller = CreateController();

        var actionResult = await controller.ExtractResume(formFileMock.Object, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(s => s.Send(It.IsAny<ExtractResumeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyExtractedProfile_DeserializesPayload_SendsCommandWithWrappedFile_AndReturnsNoContent()
    {
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("resume.pdf");
        formFileMock.Setup(f => f.ContentType).Returns("application/pdf");

        const string payload = """
        {
            "TechnicalSkills": ["C#"],
            "SoftSkills": ["Teamwork"],
            "ToolsAndPlatforms": ["Git"],
            "WorkExperiences": [],
            "Educations": []
        }
        """;

        ApplyExtractedProfileCommand? capturedCommand = null;
        _sender
            .Setup(s => s.Send(It.IsAny<ApplyExtractedProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ApplyExtractedProfileCommand, CancellationToken>((request, _) => capturedCommand = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        var actionResult = await controller.ApplyExtractedProfile(formFileMock.Object, payload, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.TechnicalSkills.Should().BeEquivalentTo(["C#"]);
        capturedCommand.SoftSkills.Should().BeEquivalentTo(["Teamwork"]);
        capturedCommand.ToolsAndPlatforms.Should().BeEquivalentTo(["Git"]);
        capturedCommand.ResumeFile.FileName.Should().Be("resume.pdf");
        _sender.Verify(s => s.Send(It.IsAny<ApplyExtractedProfileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyExtractedProfile_WhenPayloadIsMalformedJson_ThrowsBadRequestException_AndNeverSendsCommand()
    {
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("resume.pdf");
        formFileMock.Setup(f => f.ContentType).Returns("application/pdf");

        const string malformedPayload = "{ this is not valid json ";

        var controller = CreateController();

        var act = () => controller.ApplyExtractedProfile(formFileMock.Object, malformedPayload, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _sender.Verify(s => s.Send(It.IsAny<ApplyExtractedProfileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestProfileMediaUploadUrl_SendsCommand_AndReturnsOkWithTarget()
    {
        var command = new RequestProfileMediaUploadUrlCommand("image/jpeg");
        var target = new PresignedUploadTarget("https://example.com/upload", "profile-pictures/user-1/x.jpg", DateTimeOffset.UtcNow);
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var controller = CreateController();

        var actionResult = await controller.RequestProfileMediaUploadUrl(command, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(target);
    }

    [Fact]
    public async Task ConfirmProfileMediaUpload_SendsCommand_AndReturnsNoContent()
    {
        var command = new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/new.jpg");
        var controller = CreateController();

        var actionResult = await controller.ConfirmProfileMediaUpload(command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitiateMediaMultipartUpload_SendsCommand_AndReturnsOkWithResult()
    {
        var command = new InitiateMediaMultipartUploadCommand("video/mp4");
        var result = new InitiateMultipartResult("upload-1", "video-intros/user-1/x.mp4", 5 * 1024 * 1024);
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        var controller = CreateController();

        var actionResult = await controller.InitiateMediaMultipartUpload(command, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(result);
    }

    [Fact]
    public async Task RequestMediaMultipartPartUrl_SendsCommand_AndReturnsOkWithPresignedUrl()
    {
        var command = new RequestMediaMultipartPartUrlCommand("video-intros/user-1/x.mp4", "upload-1", 1);
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync("https://example.com/part-1");

        var controller = CreateController();

        var actionResult = await controller.RequestMediaMultipartPartUrl(command, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { presignedUrl = "https://example.com/part-1" });
    }

    [Fact]
    public async Task ListMediaMultipartParts_SendsQuery_AndReturnsOkWithParts()
    {
        var parts = new List<UploadedPartDto> { new(1, "\"etag-1\"", 1024) };
        _sender
            .Setup(s => s.Send(It.IsAny<ListMediaMultipartPartsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parts);

        var controller = CreateController();

        var actionResult = await controller.ListMediaMultipartParts("video-intros/user-1/x.mp4", "upload-1", CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { parts });
    }

    [Fact]
    public async Task CompleteMediaMultipartUpload_SendsCommand_AndReturnsNoContent()
    {
        var command = new CompleteMediaMultipartUploadCommand(
            "video-intros/user-1/x.mp4", "upload-1", [new CompleteMediaMultipartUploadPartItem(1, "\"etag-1\"")]);

        var controller = CreateController();

        var actionResult = await controller.CompleteMediaMultipartUpload(command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AbortMediaMultipartUpload_SendsCommand_AndReturnsNoContent()
    {
        var command = new AbortMediaMultipartUploadCommand("video-intros/user-1/x.mp4", "upload-1");
        var controller = CreateController();

        var actionResult = await controller.AbortMediaMultipartUpload(command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkExperience_SendsCommand_AndReturnsCreated()
    {
        var command = new UpsertWorkExperienceCommand(null, "Acme", "Engineer", "2020", null, true);
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(id);

        var controller = CreateController();

        var actionResult = await controller.CreateWorkExperience(command, CancellationToken.None);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task UpdateWorkExperience_SendsCommandWithRouteId_AndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var command = new UpsertWorkExperienceCommand(null, "Acme", "Engineer", "2020", null, true);

        var controller = CreateController();

        var actionResult = await controller.UpdateWorkExperience(id, command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(It.Is<UpsertWorkExperienceCommand>(c => c.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWorkExperience_SendsCommand_AndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var controller = CreateController();

        var actionResult = await controller.DeleteWorkExperience(id, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(It.Is<DeleteWorkExperienceCommand>(c => c.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEducation_SendsCommand_AndReturnsCreated()
    {
        var command = new UpsertEducationCommand(null, "State University", "BSc", "2016", "2020");
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(id);

        var controller = CreateController();

        var actionResult = await controller.CreateEducation(command, CancellationToken.None);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task UpdateEducation_SendsCommandWithRouteId_AndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var command = new UpsertEducationCommand(null, "State University", "BSc", "2016", "2020");

        var controller = CreateController();

        var actionResult = await controller.UpdateEducation(id, command, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(It.Is<UpsertEducationCommand>(c => c.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEducation_SendsCommand_AndReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var controller = CreateController();

        var actionResult = await controller.DeleteEducation(id, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(s => s.Send(It.Is<DeleteEducationCommand>(c => c.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
