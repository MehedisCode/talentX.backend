using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.JobApplications;
using TalentX.Application.Features.JobApplications.Commands.ApplyToJob;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;
using Xunit;

namespace TalentX.Api.Tests.Controllers.JobApplications;

public class JobApplicationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobApplicationsController _controller;

    public JobApplicationsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new JobApplicationsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    private static ApplyToJobCommand BodyCommand() => new(
        Guid.Empty,
        "Jane Talent",
        "jane@example.com",
        "resumes/abc_cv.pdf",
        "Keen to join.",
        90000m,
        5m,
        "Dhaka");

    [Fact]
    public async Task GetMyApplicationStatus_ReturnsOk_WithTheResult()
    {
        var jobId = Guid.NewGuid();
        var dto = new MyApplicationStatusDto(true);
        _sender
            .Setup(s => s.Send(It.Is<GetMyApplicationStatusQuery>(q => q.JobId == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetMyApplicationStatus(jobId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task ApplyToJob_SendsTheCommandWithTheRouteJobIdApplied()
    {
        var routeJobId = Guid.NewGuid();
        ApplyToJobCommand? sent = null;
        _sender
            .Setup(s => s.Send(It.IsAny<ApplyToJobCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ApplicationSubmittedDto>, CancellationToken>((req, _) => sent = (ApplyToJobCommand)req)
            .ReturnsAsync(new ApplicationSubmittedDto(Guid.NewGuid(), routeJobId, "Applied", DateTime.UtcNow));

        await _controller.ApplyToJob(routeJobId, BodyCommand(), CancellationToken.None);

        sent.Should().NotBeNull();
        sent!.JobId.Should().Be(routeJobId);
        sent.ApplicantEmail.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task ApplyToJob_Returns201Created_WithTheSubmittedDto()
    {
        var routeJobId = Guid.NewGuid();
        var dto = new ApplicationSubmittedDto(Guid.NewGuid(), routeJobId, "Applied", DateTime.UtcNow);
        _sender
            .Setup(s => s.Send(It.IsAny<ApplyToJobCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.ApplyToJob(routeJobId, BodyCommand(), CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be(dto);
    }
}
