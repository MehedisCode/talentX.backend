using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TalentX.Api.Controllers;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Domain.Enums;

namespace TalentX.Api.Tests.Api.Controllers;

public class JobsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _controller = new JobsController(_sender.Object);
    }

    private static CreateJobCommand ValidCreateCommand() => new(
        "Backend Engineer",
        "Acme Corp",
        "Build and maintain backend services.",
        EmploymentType.FullTime,
        ExperienceLevel.MidLevel,
        "Dhaka",
        WorkArrangement.Remote,
        new List<string> { "C#" },
        2,
        DateTime.UtcNow.AddDays(30),
        false
    );

    [Fact]
    public async Task CreateJob_SendsTheCommandToMediator()
    {
        var command = ValidCreateCommand();
        var jobId = Guid.NewGuid();
        _sender.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(jobId);

        await _controller.CreateJob(command);

        _sender.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJob_Returns201CreatedWithTheNewJobId()
    {
        var command = ValidCreateCommand();
        var jobId = Guid.NewGuid();
        _sender.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(jobId);

        var result = await _controller.CreateJob(command);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be(jobId);
    }

    [Fact]
    public async Task UpdateJob_SendsTheCommandWithTheRouteIdApplied()
    {
        var routeId = Guid.NewGuid();
        var command = new UpdateJobCommand(Guid.NewGuid(), "New Title", null, null, null, null, null, null, null, null, null);
        UpdateJobCommand? sentCommand = null;
        _sender
            .Setup(m => m.Send(It.IsAny<UpdateJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateJobCommand, CancellationToken>((req, _) => sentCommand = req)
            .Returns(Task.CompletedTask);

        await _controller.UpdateJob(routeId, command);

        sentCommand.Should().NotBeNull();
        sentCommand!.Id.Should().Be(routeId);
        sentCommand.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateJob_ReturnsNoContent()
    {
        var routeId = Guid.NewGuid();
        var command = new UpdateJobCommand(Guid.NewGuid(), "New Title", null, null, null, null, null, null, null, null, null);
        _sender
            .Setup(m => m.Send(It.IsAny<UpdateJobCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateJob(routeId, command);

        result.Should().BeOfType<NoContentResult>();
    }
}
