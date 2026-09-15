using FluentAssertions;
using TalentX.Application.Features.JobAlerts.Extensions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class JobAlertFilterExtensionsTests
{
    private static Job CreateJob(
        string title = "Backend Engineer",
        string location = "Dhaka",
        EmploymentType employmentType = EmploymentType.FullTime,
        ExperienceLevel experienceLevel = ExperienceLevel.MidLevel,
        WorkArrangement workArrangement = WorkArrangement.Hybrid,
        JobStatus status = JobStatus.Published,
        bool isExpired = false)
    {
        var job = Job.Create(
            "employer-1",
            title,
            "Acme",
            "Description",
            employmentType,
            experienceLevel,
            location,
            workArrangement,
            new List<string> { "C#" },
            1,
            isExpired ? DateTime.UtcNow.AddDays(-2) : DateTime.UtcNow.AddDays(30));

        if (status == JobStatus.Published)
        {
            job.MarkAsPublished();
        }

        return job;
    }

    [Fact]
    public void ApplyAlertCriteria_FiltersOutDraftAndExpiredJobs()
    {
        var alert = JobAlert.Create("talent-1", "Alert", "Engineer", null, null, null, null);

        var activeJob = CreateJob(status: JobStatus.Published, isExpired: false);
        var expiredJob = CreateJob(status: JobStatus.Published, isExpired: true);
        var draftJob = CreateJob(status: JobStatus.Draft, isExpired: false);

        var jobs = new List<Job> { activeJob, expiredJob, draftJob };

        var filtered = jobs.ApplyAlertCriteria(alert).ToList();

        filtered.Should().ContainSingle();
        filtered[0].Id.Should().Be(activeJob.Id);
    }

    [Fact]
    public void ApplyAlertCriteria_FiltersByLocationAndEmploymentType()
    {
        var alert = JobAlert.Create(
            "talent-1",
            "Alert",
            "Engineer",
            "Dhaka",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            WorkArrangement.FullRemote);

        var matchingJob = CreateJob(
            title: "Senior Lead Engineer",
            location: "Dhaka",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            workArrangement: WorkArrangement.FullRemote);

        var wrongLocation = CreateJob(
            title: "Senior Lead Engineer",
            location: "Sylhet",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            workArrangement: WorkArrangement.FullRemote);

        var wrongArrangement = CreateJob(
            title: "Senior Lead Engineer",
            location: "Dhaka",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            workArrangement: WorkArrangement.OnSite);

        var jobs = new List<Job> { matchingJob, wrongLocation, wrongArrangement };

        var filtered = jobs.ApplyAlertCriteria(alert).ToList();

        filtered.Should().ContainSingle();
        filtered[0].Id.Should().Be(matchingJob.Id);
    }
}
