using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs()
    {
        var jobs = new List<Job>();

        var job1 = new Job(Guid.NewGuid(), "Acme Inc", "Frontend Developer");
        job1.UpdateDetails(
            title: "Frontend Developer",
            companyName: "Acme Inc",
            roleType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.Mid,
            locationPref: WorkMode.Hybrid,
            city: "Dhaka",
            country: "Bangladesh",
            minOverlapHours: null,
            description: "Build and ship user-facing features across our product.",
            responsibilities: "Own feature delivery from design handoff to production release.",
            skills: new[] { "React", "Next.js", "TypeScript" },
            expiresAt: null
        );
        job1.Publish();
        jobs.Add(job1);

        var job2 = new Job(Guid.NewGuid(), "Northwind Labs", "Backend Engineer (.NET)");
        job2.UpdateDetails(
            title: "Backend Engineer (.NET)",
            companyName: "Northwind Labs",
            roleType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.Senior,
            locationPref: WorkMode.Remote,
            city: null,
            country: null,
            minOverlapHours: 4,
            description: "Own our core API and data layer.",
            responsibilities: "Design and maintain the Jobs and Employer services.",
            skills: new[] { "C#", "EF Core", "SQL Server" },
            expiresAt: null
        );
        job2.Publish();
        jobs.Add(job2);

        var job3 = new Job(Guid.NewGuid(), "Acme Inc", "Product Designer");
        job3.UpdateDetails(
            title: "Product Designer",
            companyName: "Acme Inc",
            roleType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.Mid,
            locationPref: WorkMode.OnSite,
            city: "Dhaka",
            country: "Bangladesh",
            minOverlapHours: null,
            description: "Shape the end-to-end experience for our job seekers.",
            responsibilities: null,
            skills: new[] { "Figma", "User Research" },
            expiresAt: null
        );
        job3.Publish();
        jobs.Add(job3);

        var job4 = new Job(Guid.NewGuid(), "Northwind Labs", "QA Engineer");
        job4.UpdateDetails(
            title: "QA Engineer",
            companyName: "Northwind Labs",
            roleType: EmploymentType.Contract,
            experienceLevel: ExperienceLevel.EntryLevel,
            locationPref: WorkMode.Remote,
            city: null,
            country: null,
            minOverlapHours: 3,
            description: "Own quality across our web and API surfaces.",
            responsibilities: null,
            skills: new[] { "Manual Testing", "Cypress" },
            expiresAt: null
        );
        job4.Publish();
        jobs.Add(job4);

        var job5 = new Job(Guid.NewGuid(), "Acme Inc", "Expired Internship (should NOT appear)");
        job5.UpdateDetails(
            title: "Expired Internship (should NOT appear)",
            companyName: "Acme Inc",
            roleType: EmploymentType.Internship,
            experienceLevel: ExperienceLevel.EntryLevel,
            locationPref: WorkMode.Remote,
            city: null,
            country: null,
            minOverlapHours: null,
            description: "Seeded already-expired to verify the eligibility filter works.",
            responsibilities: null,
            skills: Array.Empty<string>(),
            expiresAt: null
        );
        job5.Publish();
        job5.Expire();
        jobs.Add(job5);

        var job6 = new Job(Guid.NewGuid(), "Acme Inc", "Draft Role (should NOT appear)");
        job6.UpdateDetails(
            title: "Draft Role (should NOT appear)",
            companyName: "Acme Inc",
            roleType: EmploymentType.PartTime,
            experienceLevel: ExperienceLevel.Mid,
            locationPref: WorkMode.Hybrid,
            city: "Dhaka",
            country: "Bangladesh",
            minOverlapHours: null,
            description: "Seeded as Draft to verify it never becomes publicly visible.",
            responsibilities: null,
            skills: new[] { "SQL" },
            expiresAt: null
        );
        // Do not publish
        jobs.Add(job6);

        return jobs;
    }
}
