using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Queries.GetMyJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.Jobs.Queries;

public class GetMyJobQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetMyJobQueryHandler _handler;

    public GetMyJobQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _handler = new GetMyJobQueryHandler(_jobRepository.Object, _currentUserService.Object);
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
    public async Task Handle_ReturnsJobForOwningEmployer()
    {
        var job = CreateTestJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(job.Id);
        result.Title.Should().Be("Backend Engineer");
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new GetMyJobQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwningEmployer_ThrowsForbiddenException()
    {
        var job = CreateTestJob(employerId: "other-employer");
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var act = () => _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetMyJobQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_MapsWizardFieldsToDto()
    {
        var job = CreateTestJob();
        job.UpdateWizardDetails("Chattogram", "Bangladesh", 8, null);

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        result.City.Should().Be("Chattogram");
        result.Country.Should().Be("Bangladesh");
        result.MinOverlapHours.Should().Be(8);
    }

    [Fact]
    public async Task Handle_MapsAllJobFieldsToDto()
    {
        var job = CreateTestJob();

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        result.Id.Should().Be(job.Id);
        result.EmployerId.Should().Be("employer-1");
        result.Title.Should().Be("Backend Engineer");
        result.CompanyName.Should().Be("Acme Corp");
        result.Description.Should().Be("Build and maintain backend services.");
        result.EmploymentType.Should().Be("FullTime");
        result.ExperienceLevel.Should().Be("MidLevel");
        result.Location.Should().Be("Dhaka");
        result.WorkArrangement.Should().Be("Hybrid");
        result.RequiredSkills.Should().BeEquivalentTo(new[] { "C#", "SQL" });
        result.Status.Should().Be("Draft");
        result.Vacancies.Should().Be(2);
        result.City.Should().Be("Dhaka");
        result.Country.Should().Be("Bangladesh");
    }

    [Fact]
    public async Task Handle_DraftJob_ReturnsDraftStatus()
    {
        var job = CreateTestJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        result.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Handle_PublishedJob_ReturnsPublishedStatus()
    {
        var job = CreateTestJob();
        job.UpdateWizardDetails("Dhaka", "Bangladesh", null, null);
        job.MarkAsPublished();

        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new GetMyJobQuery(job.Id), CancellationToken.None);

        result.Status.Should().Be("Published");
    }
}
