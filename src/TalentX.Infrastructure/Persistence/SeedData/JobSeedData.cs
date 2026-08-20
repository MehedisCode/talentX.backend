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
            description: "Build and ship user-facing features across our product.",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.MidLevel,
            location: "Dhaka",
            workArrangement: WorkArrangement.Hybrid,
            requiredSkills: new[] { "React", "Next.js", "TypeScript" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );
        job1.Publish();
        jobs.Add(job1);

        var job2 = new Job(Guid.NewGuid(), "Northwind Labs", "Backend Engineer (.NET)");
        job2.UpdateDetails(
            title: "Backend Engineer (.NET)",
            companyName: "Northwind Labs",
            description: "Own our core API and data layer.",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            location: "Remote",
            workArrangement: WorkArrangement.Remote,
            requiredSkills: new[] { "C#", "EF Core", "SQL Server" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );
        job2.Publish();
        jobs.Add(job2);

        var job3 = new Job(Guid.NewGuid(), "Acme Inc", "Product Designer");
        job3.UpdateDetails(
            title: "Product Designer",
            companyName: "Acme Inc",
            description: "Shape the end-to-end experience for our job seekers.",
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.MidLevel,
            location: "Dhaka",
            workArrangement: WorkArrangement.OnSite,
            requiredSkills: new[] { "Figma", "User Research" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );
        job3.Publish();
        jobs.Add(job3);

        var job4 = new Job(Guid.NewGuid(), "Northwind Labs", "QA Engineer");
        job4.UpdateDetails(
            title: "QA Engineer",
            companyName: "Northwind Labs",
            description: "Own quality across our web and API surfaces.",
            employmentType: EmploymentType.Contract,
            experienceLevel: ExperienceLevel.EntryLevel,
            location: "Remote",
            workArrangement: WorkArrangement.Remote,
            requiredSkills: new[] { "Manual Testing", "Cypress" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );
        job4.Publish();
        jobs.Add(job4);

        // Expired job
        var job5 = new Job(
            Guid.NewGuid(),
            "Acme Inc",
            "Expired Internship (should NOT appear)"
        );

        job5.UpdateDetails(
            title: "Expired Internship (should NOT appear)",
            companyName: "Acme Inc",
            description: "Seeded already-expired to verify the eligibility filter works.",
            employmentType: EmploymentType.Internship,
            experienceLevel: ExperienceLevel.EntryLevel,
            location: "Remote",
            workArrangement: WorkArrangement.Remote,
            requiredSkills: new[] { "Java", "Spring Boot" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );

        job5.Publish();
        job5.Expire();
        jobs.Add(job5);

        // Draft job
        var job6 = new Job(
            Guid.NewGuid(),
            "Acme Inc",
            "Draft Role (should NOT appear)"
        );

        job6.UpdateDetails(
            title: "Draft Role (should NOT appear)",
            companyName: "Acme Inc",
            description: "Seeded as Draft to verify it never becomes publicly visible.",
            employmentType: EmploymentType.PartTime,
            experienceLevel: ExperienceLevel.MidLevel,
            location: "Dhaka",
            workArrangement: WorkArrangement.Hybrid,
            requiredSkills: new[] { "SQL" }.ToList(),
            vacancies: 1,
            expireAt: DateTime.UtcNow.AddMonths(1)
        );

        jobs.Add(job6);

        return jobs;
    }
}