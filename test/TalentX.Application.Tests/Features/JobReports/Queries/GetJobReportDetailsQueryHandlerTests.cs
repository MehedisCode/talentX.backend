using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;
using TalentX.Domain.Entities;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobReports.Queries;

public class GetJobReportDetailsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly GetJobReportDetailsQueryHandler _handler;

    private readonly Mock<IJobReportRepository> _jobReportRepository;
    private readonly Mock<IUserRepository> _userRepository;

    public GetJobReportDetailsQueryHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        
        _jobReportRepository = new Mock<IJobReportRepository>();
        _userRepository = new Mock<IUserRepository>();

        _unitOfWork.SetupGet(u => u.JobReports).Returns(_jobReportRepository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_userRepository.Object);

        _handler = new GetJobReportDetailsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ReportNotFound_ThrowsNotFoundException()
    {
        var reportId = Guid.NewGuid();
        _jobReportRepository.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobReport?)null);

        var query = new GetJobReportDetailsQuery(reportId);

        var action = () => _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReportFound_ReturnsFullJobReportDetailsDto()
    {
        var jobId = Guid.NewGuid();
        var job = (Job)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Job)); job.GetType().GetProperty("Title")?.SetValue(job, "Job Title"); job.GetType().GetProperty("EmployerId")?.SetValue(job, "employer-1");
        job.GetType().GetProperty("CompanyName")?.SetValue(job, "Company Name");

        var report = new JobReport(jobId, "reporter-1", JobReportReason.SuspectedScam, "Details");
        report.GetType().GetProperty("Job")?.SetValue(report, job);
        report.Resolve("admin-1", "Admin Notes");

        _jobReportRepository.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var reporter = new User("reporter-1", "Reporter Name", "reporter@test.com", TalentX.Domain.Enums.UserRole.Employer);
        var admin = new User("admin-1", "Admin Name", "admin@test.com", TalentX.Domain.Enums.UserRole.Admin);

        _userRepository.Setup(r => r.GetByUserIdAsync("reporter-1"))
            .ReturnsAsync(reporter);
        _userRepository.Setup(r => r.GetByUserIdAsync("admin-1"))
            .ReturnsAsync(admin);

        var query = new GetJobReportDetailsQuery(report.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(report.Id);
        result.JobId.Should().Be(report.JobId);
        result.JobTitle.Should().Be("Job Title");
        result.CompanyName.Should().Be("Company Name");
        result.Reason.Should().Be(JobReportReason.SuspectedScam);
        result.ReasonLabel.Should().Be("Suspected scam or fraud");
        result.Status.Should().Be(JobReportStatus.Resolved);
        result.AdditionalDetails.Should().Be("Details");
        result.AdminNotes.Should().Be("Admin Notes");
        result.ReporterUserId.Should().Be("reporter-1");
        result.ReporterEmail.Should().Be("reporter@test.com");
        result.ResolvedByUserId.Should().Be("admin-1");
        result.ResolvedByEmail.Should().Be("admin@test.com");
        result.ResolvedAt.Should().Be(report.ResolvedAt);
        result.CreatedAt.Should().Be(report.CreatedAt);
        result.JobStatus.Should().Be(job.Status);
    }
}
