using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TalentX.API.Controllers;
using TalentX.API.Controllers.Admin;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.JobReports.Commands.DismissJobReport;
using TalentX.Application.Features.JobReports.Commands.ResolveJobReport;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;
using TalentX.Application.Features.JobReports.Queries.GetJobReports;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Admin;

public class JobReportsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly JobReportsController _controller;

    public JobReportsControllerTests()
    {
        _senderMock = new Mock<ISender>();
        
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(_senderMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new JobReportsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void JobReportsController_Class_InheritsBaseApiController()
    {
        typeof(JobReportsController).Should().BeDerivedFrom<BaseApiController>();
    }

    [Fact]
    public void JobReportsController_Class_RequiresAdminRole()
    {
        var attributes = typeof(JobReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attributes.Should().ContainSingle();
        var authorizeAttribute = (AuthorizeAttribute)attributes[0];
        authorizeAttribute.Roles.Should().Be("Admin");
    }

    [Fact]
    public void JobReportsController_Class_HasRouteAttributeWithExpectedTemplate()
    {
        var attributes = typeof(JobReportsController).GetCustomAttributes(typeof(RouteAttribute), true);
        attributes.Should().ContainSingle();
        var routeAttribute = (RouteAttribute)attributes[0];
        routeAttribute.Template.Should().Be("api/v1/admin/job-reports");
    }

    [Fact]
    public async Task GetReports_ValidState_ReturnsOk()
    {
        var queryParams = new AdminJobReportQueryParams { Page = 1, PageSize = 10 };
        var paginatedList = new PaginatedList<JobReportListItemDto>(new System.Collections.Generic.List<JobReportListItemDto>(), 1, 10, 0);

        _senderMock.Setup(s => s.Send(It.IsAny<GetJobReportsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        var result = await _controller.GetReports(queryParams, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(paginatedList);

        _senderMock.Verify(s => s.Send(It.Is<GetJobReportsQuery>(q => 
            q.Page == 1 && q.PageSize == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReportDetails_ValidState_ReturnsOk()
    {
        var reportId = Guid.NewGuid();
        var detailsDto = new JobReportDetailsDto(reportId, Guid.NewGuid(), "Job", "Company", JobReportReason.SuspectedScam, "Spam", JobReportStatus.Pending, "Details", null, "reporter-1", "reporter@test.com", null, null, null, DateTime.UtcNow, JobStatus.Published);

        _senderMock.Setup(s => s.Send(It.IsAny<GetJobReportDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailsDto);

        var result = await _controller.GetReportDetails(reportId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(detailsDto);

        _senderMock.Verify(s => s.Send(It.Is<GetJobReportDetailsQuery>(q => q.ReportId == reportId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveReport_ValidState_ReturnsOk()
    {
        var reportId = Guid.NewGuid();
        var requestDto = new ResolveJobReportRequestDto(DeactivationReason.Spam, "Spam details", "Admin notes");
        var responseDto = new StatusUpdateResponseDto("Job report resolved and job deactivated.");

        _senderMock.Setup(s => s.Send(It.IsAny<ResolveJobReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.ResolveReport(reportId, requestDto, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(responseDto);

        _senderMock.Verify(s => s.Send(It.Is<ResolveJobReportCommand>(c => 
            c.ReportId == reportId && 
            c.DeactivationReason == DeactivationReason.Spam && 
            c.Details == "Spam details" && 
            c.AdminNotes == "Admin notes"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DismissReport_ValidState_ReturnsOk()
    {
        var reportId = Guid.NewGuid();
        var requestDto = new DismissJobReportRequestDto("Not spam");
        var responseDto = new StatusUpdateResponseDto("Job report dismissed.");

        _senderMock.Setup(s => s.Send(It.IsAny<DismissJobReportCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.DismissReport(reportId, requestDto, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().Be(responseDto);

        _senderMock.Verify(s => s.Send(It.Is<DismissJobReportCommand>(c => 
            c.ReportId == reportId && 
            c.AdminNotes == "Not spam"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
