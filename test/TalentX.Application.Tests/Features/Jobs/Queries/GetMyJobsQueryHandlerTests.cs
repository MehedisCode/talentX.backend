using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Queries.GetMyJobs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetMyJobsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetMyJobsQueryHandler _handler;

    public GetMyJobsQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _handler = new GetMyJobsQueryHandler(_jobRepository.Object, _currentUserService.Object);
    }

    private static Job CreateTestJob(string employerId = "employer-1", string title = "Backend Engineer")
    {
        var job = Job.Create(
            employerId,
            title,
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#", "SQL" },
            2,
            DateTime.UtcNow.AddDays(30)
        );

        job.UpdateWizardDetails("Dhaka", "Bangladesh", null, null);

        return job;
    }

    [Fact]
    public async Task Handle_ReturnsJobsForEmployer()
    {
        var jobs = new List<Job> { CreateTestJob(), CreateTestJob(title: "Frontend Engineer") };
        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        var result = await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(j => j.Title == "Backend Engineer");
        result.Should().Contain(j => j.Title == "Frontend Engineer");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoJobs()
    {
        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        var result = await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_MapsWizardFieldsToDto()
    {
        var job = CreateTestJob();
        job.UpdateWizardDetails("Chattogram", "Bangladesh", 6, null);

        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        var dto = result.First();
        dto.City.Should().Be("Chattogram");
        dto.Country.Should().Be("Bangladesh");
        dto.MinOverlapHours.Should().Be(6);
    }

    [Fact]
    public async Task Handle_MapsAllJobFieldsToDto()
    {
        var job = CreateTestJob();

        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        var dto = result.First();
        dto.Id.Should().Be(job.Id);
        dto.EmployerId.Should().Be("employer-1");
        dto.Title.Should().Be("Backend Engineer");
        dto.CompanyName.Should().Be("Acme Corp");
        dto.EmploymentType.Should().Be("FullTime");
        dto.ExperienceLevel.Should().Be("MidLevel");
        dto.WorkArrangement.Should().Be("Hybrid");
        dto.Status.Should().Be("Draft");
        dto.Vacancies.Should().Be(2);
        dto.RequiredSkills.Should().BeEquivalentTo(new[] { "C#", "SQL" });
    }

    [Fact]
    public async Task Handle_OnlyReturnsJobsForCurrentEmployer()
    {
        var myJobs = new List<Job> { CreateTestJob("employer-1") };
        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(myJobs);

        await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        _jobRepository.Verify(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsDeactivationFieldsOnlyForDeactivatedJobs()
    {
        var deactivated = CreateTestJob(title: "Deactivated Job");
        deactivated.MarkAsPublished();
        deactivated.Deactivate(DeactivationReason.DuplicatePosting, "Same role posted three times this week.");
        var live = CreateTestJob(title: "Live Job");
        live.MarkAsPublished();
        _jobRepository.Setup(r => r.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { deactivated, live });

        var result = await _handler.Handle(new GetMyJobsQuery(), CancellationToken.None);

        var deactivatedDto = result.Single(j => j.Title == "Deactivated Job");
        deactivatedDto.DeactivationReasonLabel.Should().Be("Duplicate posting");
        deactivatedDto.DeactivationDetails.Should().Be("Same role posted three times this week.");
        deactivatedDto.DeactivatedAt.Should().NotBeNull();

        var liveDto = result.Single(j => j.Title == "Live Job");
        liveDto.DeactivationReasonLabel.Should().BeNull();
        liveDto.DeactivationDetails.Should().BeNull();
    }
}
