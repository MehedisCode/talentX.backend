using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Employers;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;
using TalentX.Application.Features.Employers.Queries.GetEmployerJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Employers;

public sealed class EmployersControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly EmployersController _controller;

    public EmployersControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_sender.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new EmployersController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void GetDashboard_HasAuthorizeAttribute_WithEmployerRole()
    {
        var method = typeof(EmployersController).GetMethod(nameof(EmployersController.GetDashboard));
        var attribute = method?.GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be(nameof(UserRole.Employer));
    }

    [Fact]
    public void GetJobs_HasAuthorizeAttribute_WithEmployerRole()
    {
        var method = typeof(EmployersController).GetMethod(nameof(EmployersController.GetJobs));
        var attribute = method?.GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be(nameof(UserRole.Employer));
    }

    [Fact]
    public async Task GetDashboard_SendsQueryAndReturnsOk()
    {
        var expectedDashboard = new EmployerDashboardDto(
            TotalPostedJobs: 5,
            TotalHires: 2,
            ActiveJobsCount: 3,
            DraftJobsCount: 1,
            ClosedJobsCount: 1,
            ExpiredJobsCount: 0,
            DeactivatedJobsCount: 0,
            PendingReviewJobsCount: 0,
            CandidatePipeline: new CandidatePipelineStatsDto(5, 3, 2, 4, 1, 2),
            RecentJobs: []);

        _sender
            .Setup(sender => sender.Send(It.IsAny<GetEmployerDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDashboard);

        var actionResult = await _controller.GetDashboard(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedDashboard);
        _sender.Verify(sender => sender.Send(It.IsAny<GetEmployerDashboardQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobs_SendsQueryAndReturnsOk()
    {
        var jobItems = new List<EmployerJobListItemDto>
        {
            new(
                Guid.NewGuid(),
                "Senior Backend Engineer",
                "Acme Corp",
                JobStatus.Published.ToString(),
                EmploymentType.FullTime,
                WorkArrangement.FullRemote,
                ExperienceLevel.SeniorLevel,
                "Dhaka",
                2,
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow,
                5,
                SalaryType: null,
                SalaryMin: null,
                SalaryMax: null,
                SalaryCurrency: null,
                SalaryPeriod: null)
        };
        var expectedPaginatedList = new PaginatedList<EmployerJobListItemDto>(jobItems, 1, 10, 1);

        GetEmployerJobsQuery? capturedQuery = null;
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetEmployerJobsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PaginatedList<EmployerJobListItemDto>>, CancellationToken>((request, _) =>
                capturedQuery = (GetEmployerJobsQuery)request)
            .ReturnsAsync(expectedPaginatedList);

        var queryParams = new GetEmployerJobsQueryParams(1, 10, JobStatus.Published, false, null);
        var actionResult = await _controller.GetJobs(queryParams, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedPaginatedList);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.Page.Should().Be(1);
        capturedQuery.PageSize.Should().Be(10);
        capturedQuery.Status.Should().Be(JobStatus.Published);
        capturedQuery.IsExpired.Should().Be(false);
        capturedQuery.SearchTerm.Should().BeNull();
        _sender.Verify(sender => sender.Send(It.IsAny<GetEmployerJobsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobs_WithSearchTerm_BindsAndSendsSearchTermInQuery()
    {
        var expectedPaginatedList = new PaginatedList<EmployerJobListItemDto>([], 1, 10, 0);

        GetEmployerJobsQuery? capturedQuery = null;
        _sender
            .Setup(sender => sender.Send(It.IsAny<GetEmployerJobsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PaginatedList<EmployerJobListItemDto>>, CancellationToken>((request, _) =>
                capturedQuery = (GetEmployerJobsQuery)request)
            .ReturnsAsync(expectedPaginatedList);

        var queryParams = new GetEmployerJobsQueryParams(1, 10, JobStatus.Published, false, "Backend");
        var actionResult = await _controller.GetJobs(queryParams, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedPaginatedList);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.SearchTerm.Should().Be("Backend");
        _sender.Verify(sender => sender.Send(It.IsAny<GetEmployerJobsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsOk_WithEmployerName()
    {
        var currentUserServiceMock = new Mock<TalentX.Application.Identity.Services.ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns("emp-user-123");

        var employer = new TalentX.Domain.Employer("emp-user-123", "Fariha Jabin", "fariha.jabin@example.com", "Acme Corp");
        var employerRepositoryMock = new Mock<TalentX.Application.Common.Interfaces.Repositories.IEmployerRepository>();
        employerRepositoryMock
            .Setup(r => r.GetByUserIdAsync("emp-user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employer);

        var actionResult = await _controller.GetMyProfile(currentUserServiceMock.Object, employerRepositoryMock.Object, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var resultDto = okResult.Value.Should().BeOfType<CurrentEmployerResult>().Subject;
        resultDto.Name.Should().Be("Fariha Jabin");
        resultDto.CompanyName.Should().Be("Acme Corp");
    }
}
