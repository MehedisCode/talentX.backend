using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs()
    {
        var now = DateTime.UtcNow;

        var jobs = new List<Job>
        {
            // ==========================================
            // ACME INC (Fintech & Cloud SaaS - HQ: Dhaka)
            // ==========================================
            Job.Create(
                "seed-acme", "Frontend Developer", "Acme Inc",
                "Build and ship responsive user-facing features across our product suite using React 19, Next.js, and TypeScript.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "React", "Next.js", "TypeScript" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-northwind", "Backend Engineer (.NET)", "Northwind Labs",
                "Own our core API and data layer.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote", WorkArrangement.FullRemote,
                new List<string> { "C#", "EF Core", "SQL Server" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-acme", "Product Designer", "Acme Inc",
                "Shape the end-to-end experience for our job discovery platform, creating intuitive user flows and design systems in Figma.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Figma", "User Research", "Design Systems", "Prototyping" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "QA Engineer", "Northwind Labs",
                "Own quality across our web and API surfaces.",
                EmploymentType.Contract, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.FullRemote,
                new List<string> { "Manual Testing", "Cypress" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-acme", "Expired Internship (should NOT appear)", "Acme Inc",
                "Seeded already-expired to verify the eligibility filter works.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.FullRemote,
                new List<string> { "Java", "Spring Boot" },
                1, now.AddDays(-2)
            ),
            // Intentionally left as Draft — must NEVER appear in public search results
            Job.Create(
                "seed-acme", "Draft SQL Specialist Role (should NOT appear)", "Acme Inc",
                "Seeded as Draft to verify it never becomes publicly visible.",
                EmploymentType.PartTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "SQL", "Database Tuning" },
                1, now.AddMonths(1)
            )
        };

        // Publish all except the very last one (which remains Draft to test Status filtering)
        for (var i = 0; i < jobs.Count - 1; i++)
        {
            jobs[i].MarkAsPublished();
        }

        return jobs;
    }
}
