using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.JobApplications;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;
using Xunit;

namespace TalentX.Api.Tests.Controllers.JobApplications;

public class JobApplicationDetailsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly JobApplicationDetailsController _controller;

    public JobApplicationDetailsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new JobApplicationDetailsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    private static JobApplicationDetailsDto Dto(Guid id) => new(
        id,
        Guid.NewGuid(),
        "Jane Talent",
        "jane@example.com",
        "resumes/abc_cv.pdf",
        "I am a great fit.",
        90000m,
        5m,
        "Dhaka",
        "Applied",
        DateTime.UtcNow,
        null);

    [Fact]
    public async Task GetById_SendsQueryWithTheRouteId()
    {
        var id = Guid.NewGuid();
        _sender
            .Setup(s => s.Send(It.Is<GetJobApplicationByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dto(id));

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(
            s => s.Send(It.Is<GetJobApplicationByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WithTheDto()
    {
        var id = Guid.NewGuid();
        var dto = Dto(id);
        _sender
            .Setup(s => s.Send(It.IsAny<GetJobApplicationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }
}
