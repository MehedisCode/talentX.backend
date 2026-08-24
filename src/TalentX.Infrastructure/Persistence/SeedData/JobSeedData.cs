using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs()
    {
        var jobs = new List<Job>
        {
            Job.Create(
                "seed-acme", "Frontend Developer", "Acme Inc",
                "Build and ship user-facing features across our product.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "React", "Next.js", "TypeScript" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-northwind", "Backend Engineer (.NET)", "Northwind Labs",
                "Own our core API and data layer.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "C#", "EF Core", "SQL Server" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-acme", "Product Designer", "Acme Inc",
                "Shape the end-to-end experience for our job seekers.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Figma", "User Research" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-northwind", "QA Engineer", "Northwind Labs",
                "Own quality across our web and API surfaces.",
                EmploymentType.Contract, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Manual Testing", "Cypress" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            // Already-expired on purpose, to verify the "published but expired" eligibility filter works.
            Job.Create(
                "seed-acme", "Expired Internship (should NOT appear)", "Acme Inc",
                "Seeded already-expired to verify the eligibility filter works.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Java", "Spring Boot" },
                1, DateTime.UtcNow.AddDays(-1)
            ),
            // Left as Draft on purpose, to verify it never becomes publicly visible.
            Job.Create(
                "seed-acme", "Draft Role (should NOT appear)", "Acme Inc",
                "Seeded as Draft to verify it never becomes publicly visible.",
                EmploymentType.PartTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "SQL" },
                1, DateTime.UtcNow.AddMonths(1)
            )
        };

        // First four are published so they show up in the public listing; the
        // expired one publishes too (to prove the filter excludes it on ExpireAt,
        // not on Status) — the last one stays a Draft.
        for (var i = 0; i < 5; i++)
        {
            jobs[i].MarkAsPublished();
        }

        return jobs;
    }
}
