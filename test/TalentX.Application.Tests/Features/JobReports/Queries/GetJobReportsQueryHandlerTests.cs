using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobReports.Queries.GetJobReports;
using TalentX.Domain.Entities;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobReports.Queries;

public class GetJobReportsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly GetJobReportsQueryHandler _handler;

    private readonly Mock<IJobReportRepository> _jobReportRepository;
    private readonly Mock<IUserRepository> _userRepository;

    public GetJobReportsQueryHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        
        _jobReportRepository = new Mock<IJobReportRepository>();
        _userRepository = new Mock<IUserRepository>();

        _unitOfWork.SetupGet(u => u.JobReports).Returns(_jobReportRepository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);

        _handler = new GetJobReportsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidState_ReturnsPaginatedListOfJobReportListItemDto()
    {
        var jobId = Guid.NewGuid();
        var job = (Job)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Job)); job.GetType().GetProperty("Title")?.SetValue(job, "Job Title"); job.GetType().GetProperty("EmployerId")?.SetValue(job, "employer-1");
        job.GetType().GetProperty("CompanyName")?.SetValue(job, "Company Name");

        var report = new JobReport(jobId, "reporter-1", JobReportReason.SuspectedScam, "Details");
        report.GetType().GetProperty("Job")?.SetValue(report, job);

        var paginatedReports = new PaginatedList<JobReport>(new List<JobReport> { report }, 1, 10, 1);

        _jobReportRepository.Setup(r => r.GetReportsAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedReports);

        var reporter = new User("reporter-1", "Reporter Name", "reporter@test.com", TalentX.Domain.Enums.UserRole.Employer);
        _userRepository.Setup(r => r.GetByUserIdAsync("reporter-1"))
            .ReturnsAsync(reporter);

        var query = new GetJobReportsQuery { Page = 1, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);

        var item = result.Items[0];
        item.Id.Should().Be(report.Id);
        item.JobId.Should().Be(report.JobId);
        item.JobTitle.Should().Be("Job Title");
        item.CompanyName.Should().Be("Company Name");
        item.Reason.Should().Be(JobReportReason.SuspectedScam);
        item.ReasonLabel.Should().Be("Suspected scam or fraud");
        item.Status.Should().Be(JobReportStatus.Pending);
        item.CreatedAt.Should().Be(report.CreatedAt);
        item.ReporterEmail.Should().Be("reporter@test.com");
    }
}
