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
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Jobs.Commands.DeactivateJob;
using TalentX.Application.Features.Jobs.Commands.ReactivateJob;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobs;
using TalentX.Domain.Enums;
using Xunit;
// The employer controller in TalentX.API.Controllers has the same class name.
using AdminJobsController = TalentX.API.Controllers.Admin.JobsController;

namespace TalentX.Api.Tests.Controllers.Admin;

public sealed class JobsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly AdminJobsController _controller;

    public JobsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new AdminJobsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void JobReportsController_Class_InheritsBaseApiController()
    {
        typeof(AdminJobsController).BaseType.Should().Be(typeof(BaseApiController));
    }

    [Fact]
    public void JobReportsController_Class_RequiresAdminRoleForEveryAction()
    {
        // Declared once on the class, so it covers all four actions.
        var authorizeAttribute = typeof(AdminJobsController).GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void JobReportsController_Class_HasRouteAttributeWithExpectedTemplate()
    {
        var routeAttribute = typeof(AdminJobsController).GetCustomAttribute<RouteAttribute>();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/v1/admin/jobs");
    }

    [Fact]
    public async Task GetJobs_WithQuery_DispatchesToSenderAndReturnsOk()
    {
        var query = new GetAdminJobsQuery { SearchQuery = "acme", Status = JobStatus.Deactivated, Page = 1, PageSize = 20 };
        var expectedResult = new PaginatedList<AdminJobListItemDto>(
            new List<AdminJobListItemDto> { new() { Id = Guid.NewGuid(), Title = "Backend Engineer", Status = JobStatus.Deactivated } },
            1,
            20,
            1);
        _sender
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetJobs(query, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobs_WithNullQuery_DispatchesDefaultQueryAndReturnsOk()
    {
        var expectedResult = new PaginatedList<AdminJobListItemDto>(new List<AdminJobListItemDto>(), 1, 20, 0);
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetAdminJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetJobs(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _sender.Verify(
            sender => sender.Send(
                It.Is<GetAdminJobsQuery>(q => q.SearchQuery == null && q.Status == null && q.Page == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetJobDetails_WithId_DispatchesQueryWithMatchingJobIdAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var expectedResult = new AdminJobDetailsDto { Id = jobId, Title = "Backend Engineer", Status = JobStatus.Published };
        _sender
            .Setup(sender => sender.Send(It.Is<GetAdminJobDetailsQuery>(q => q.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetJobDetails(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(It.Is<GetAdminJobDetailsQuery>(q => q.JobId == jobId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateJob_UsesTheRouteIdEvenWhenTheBodyCarriesADifferentJobId()
    {
        var routeJobId = Guid.NewGuid();
        var bodyJobId = Guid.NewGuid();
        var command = new DeactivateJobCommand(bodyJobId, DeactivationReason.Spam, "Posting links to an unrelated external site.");
        var expectedResult = new StatusUpdateResponseDto("Job deactivated.");
        _sender
            .Setup(sender => sender.Send(It.IsAny<DeactivateJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.DeactivateJob(routeJobId, command, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(
                It.Is<DeactivateJobCommand>(c =>
                    c.JobId == routeJobId &&
                    c.Reason == DeactivationReason.Spam &&
                    c.Details == "Posting links to an unrelated external site."),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sender.Verify(
            sender => sender.Send(It.Is<DeactivateJobCommand>(c => c.JobId == bodyJobId), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReactivateJob_WithId_DispatchesCommandWithMatchingJobIdAndReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var expectedResult = new StatusUpdateResponseDto("Job reactivated.");
        _sender
            .Setup(sender => sender.Send(It.Is<ReactivateJobCommand>(c => c.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _controller.ReactivateJob(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResult);
        _sender.Verify(
            sender => sender.Send(It.Is<ReactivateJobCommand>(c => c.JobId == jobId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
