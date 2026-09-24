using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.Commands.CloseJob;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.DeleteJob;
using TalentX.Application.Features.Jobs.Commands.ReopenJob;
using TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;
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
        WorkArrangement.FullRemote,
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
        var paginatedList = new PaginatedList<JobListItemDto>(new List<JobListItemDto>(), 1, 10, 0);
        _sender.Setup(s => s.Send(It.IsAny<GetPublishedJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        var result = await _controller.GetJobs();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(paginatedList);
    }

    [Fact]
    public async Task GetJobs_WithFilterParams_SendsQueryWithCorrectFilters()
    {
        var paginatedList = new PaginatedList<JobListItemDto>(new List<JobListItemDto>(), 1, 10, 0);
        GetPublishedJobsQuery? sentQuery = null;

        _sender
            .Setup(s => s.Send(It.IsAny<GetPublishedJobsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PaginatedList<JobListItemDto>>, CancellationToken>((req, _) =>
                sentQuery = (GetPublishedJobsQuery)req)
            .ReturnsAsync(paginatedList);

        var queryParams = new GetJobsQueryParams(
            Keyword: "engineer",
            Location: "Dhaka",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.SeniorLevel,
            WorkArrangement: WorkArrangement.Hybrid,
            Page: 2,
            PageSize: 10);

        await _controller.GetJobs(queryParams);

        sentQuery.Should().NotBeNull();
        sentQuery!.Keyword.Should().Be("engineer");
        sentQuery.Location.Should().Be("Dhaka");
        sentQuery.EmploymentType.Should().Be(EmploymentType.FullTime);
        sentQuery.ExperienceLevel.Should().Be(ExperienceLevel.SeniorLevel);
        sentQuery.WorkArrangement.Should().Be(WorkArrangement.Hybrid);
        sentQuery.Page.Should().Be(2);
        sentQuery.PageSize.Should().Be(10);
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
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("resume.pdf");
        formFileMock.Setup(f => f.Length).Returns(1024);
        formFileMock.Setup(f => f.ContentType).Returns("application/pdf");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([0x25, 0x50, 0x44, 0x46]));

        var requestDto = new ApplyToJobRequestDto(
            "Jane Doe",
            "jane@example.com",
            formFileMock.Object,
            "I am excited to apply for this role.",
            85000,
            6,
            "Dhaka"
        );
        ApplyToJobCommand? sentCommand = null;
        var response = new ApplyToJobResponse(Guid.NewGuid(), routeId);
        _sender
            .Setup(s => s.Send(It.IsAny<ApplyToJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ApplyToJobResponse>, CancellationToken>((req, _) => sentCommand = (ApplyToJobCommand)req)
            .ReturnsAsync(response);

        var result = await _controller.Apply(routeId, requestDto, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
        sentCommand.Should().NotBeNull();
        sentCommand!.JobId.Should().Be(routeId);
        sentCommand.ApplicantName.Should().Be("Jane Doe");
        sentCommand.ResumeFile.FileName.Should().Be("resume.pdf");
    }

    [Fact]
    public async Task CloseJob_SendsCommandAndReturnsNoContent()
    {
        var jobId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send(It.IsAny<CloseJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.CloseJob(jobId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(sender => sender.Send(new CloseJobCommand(jobId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteJob_SendsCommandAndReturnsNoContent()
    {
        var jobId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send(It.IsAny<DeleteJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.DeleteJob(jobId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(sender => sender.Send(new DeleteJobCommand(jobId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReopenJob_SendsCommandAndReturnsNoContent()
    {
        var jobId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send(It.IsAny<ReopenJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.ReopenJob(jobId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(sender => sender.Send(new ReopenJobCommand(jobId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResubmitJobForReview_SendsCommandAndReturnsNoContent()
    {
        var jobId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send(It.IsAny<ResubmitJobForReviewCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var actionResult = await _controller.ResubmitJobForReview(jobId, CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        _sender.Verify(
            sender => sender.Send(new ResubmitJobForReviewCommand(jobId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ResubmitJobForReview_IsRestrictedToEmployers()
    {
        var authorizeAttribute = typeof(JobsController)
            .GetMethod(nameof(JobsController.ResubmitJobForReview))!
            .GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Employer));
    }

    [Fact]
    public async Task ReportJob_ValidState_ReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var request = new TalentX.Application.Features.JobReports.DTOs.ReportJobRequestDto(TalentX.Domain.Enums.JobReportReason.SuspectedScam, "Scam");
        var expectedResponse = new TalentX.Application.Features.JobReports.DTOs.JobReportConfirmationDto(Guid.NewGuid(), "Success");
        
        _sender
            .Setup(sender => sender.Send(It.IsAny<TalentX.Application.Features.JobReports.Commands.ReportJob.ReportJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _controller.ReportJob(jobId, request, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
