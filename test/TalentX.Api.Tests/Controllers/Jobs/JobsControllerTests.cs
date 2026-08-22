using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TalentX.API.Controllers.Jobs;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

namespace TalentX.API.Tests.Controllers.Jobs;

public class JobsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _senderMock = new Mock<ISender>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(s => s.GetService(typeof(ISender))).Returns(_senderMock.Object);

        var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
        _controller = new JobsController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
    }

    [Fact]
    public async Task GetJobs_ReturnsOkResult_WithPaginatedList()
    {
        // Arrange
        var paginatedList = new PaginatedList<JobListItemDto>(new List<JobListItemDto>(), 1, 20, 0);
        _senderMock.Setup(s => s.Send(It.IsAny<GetPublishedJobsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await _controller.GetJobs();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(paginatedList);
    }

    [Fact]
    public async Task GetJobById_ReturnsOkResult_WithJobDetails()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobDetails = new JobDetailsDto { Id = jobId, Title = "Test Job" };
        _senderMock.Setup(s => s.Send(It.Is<GetJobDetailsQuery>(q => q.Id == jobId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobDetails);

        // Act
        var result = await _controller.GetJobById(jobId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(jobDetails);
    }
}
