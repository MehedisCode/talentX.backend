using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.JobApplications;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.JobApplications;

public class EmployerJobApplicationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly EmployerJobApplicationsController _controller;

    public EmployerJobApplicationsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var provider = services.BuildServiceProvider();

        _controller = new EmployerJobApplicationsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }

    [Fact]
    public async Task GetJobApplicants_SendsQueryToMediator_ReturnsOkWithPaginatedResult()
    {
        var jobId = Guid.NewGuid();
        var item = new JobApplicantListItemDto(
            Guid.NewGuid(),
            jobId,
            "talent-1",
            "Alice Smith",
            "alice@example.com",
            "https://storage.test/resume.pdf",
            "Applied",
            85.5m,
            100000m,
            5m,
            "Dhaka",
            DateTime.UtcNow);

        var paginatedResult = new PaginatedList<JobApplicantListItemDto>(
            new List<JobApplicantListItemDto> { item },
            1,
            10,
            1);

        _sender
            .Setup(s => s.Send(
                It.Is<GetJobApplicantsQuery>(q => q.JobId == jobId && q.Search == "Alice" && q.Status == ApplicationStatus.Applied && q.SortBy == null && q.SortOrder == null && q.Page == 1 && q.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        var result = await _controller.GetJobApplicants(
            jobId,
            search: "Alice",
            status: ApplicationStatus.Applied,
            sortBy: null,
            sortOrder: null,
            page: 1,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(paginatedResult);
    }
}
