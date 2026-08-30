using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Jobs;

public class JobsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new JobsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
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
    public async Task CreateJob_SendsTheCommandToSender()
    {
        var command = ValidCreateCommand();
        var jobId = Guid.NewGuid();
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(jobId);

        await _controller.CreateJob(command);

        _sender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJob_Returns201CreatedWithTheNewJobId()
    {
        var command = ValidCreateCommand();
        var jobId = Guid.NewGuid();
        _sender.Setup(s => s.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(jobId);

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
            .Setup(s => s.Send(It.IsAny<UpdateJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => sentCommand = (UpdateJobCommand)req)
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
            .Setup(s => s.Send(It.IsAny<UpdateJobCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateJob(routeId, command);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetJobs_ReturnsOkResult_WithPaginatedList()
    {
        var paginatedList = new PaginatedList<JobListItemDto>(new List<JobListItemDto>(), 1, 20, 0);
        _sender.Setup(s => s.Send(It.IsAny<GetPublishedJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        var result = await _controller.GetJobs();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(paginatedList);
    }

    [Fact]
    public async Task GetJobById_ReturnsOkResult_WithJobDetails()
    {
        var jobId = Guid.NewGuid();
        var jobDetails = new JobDetailsDto { Id = jobId, Title = "Test Job" };
        _sender.Setup(s => s.Send(It.Is<GetJobDetailsQuery>(q => q.Id == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobDetails);

        var result = await _controller.GetJobById(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(jobDetails);
    }

    [Fact]
    public async Task Apply_SendsTheCommandWithTheRouteIdApplied()
    {
        var routeId = Guid.NewGuid();
        var command = new ApplyToJobCommand(
            Guid.NewGuid(),
            "Jane Doe",
            "jane@example.com",
            "uploads/1234/resume.pdf",
            "I am excited to apply for this role.",
            85000,
            6,
            "Dhaka"
        );
        ApplyToJobCommand? sentCommand = null;
        var response = new ApplyToJobResponse(Guid.NewGuid(), routeId, false);
        _sender
            .Setup(s => s.Send(It.IsAny<ApplyToJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ApplyToJobResponse>, CancellationToken>((req, _) => sentCommand = (ApplyToJobCommand)req)
            .ReturnsAsync(response);

        var result = await _controller.Apply(routeId, command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
        sentCommand.Should().NotBeNull();
        sentCommand!.JobId.Should().Be(routeId);
        sentCommand.ApplicantName.Should().Be("Jane Doe");
    }
}
