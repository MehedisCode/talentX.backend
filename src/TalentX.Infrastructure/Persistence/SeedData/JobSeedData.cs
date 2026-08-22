using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs() => new()
    {
        new Job
        {
            EmployerId = "seed-acme", Title = "Frontend Developer", CompanyName = "Acme Inc",
            Description = "Build and ship user-facing features across our product.",
            EmploymentType = EmploymentType.FullTime, ExperienceLevel = ExperienceLevel.MidLevel,
            Location = "Dhaka", WorkMode = WorkMode.Hybrid,
            RequiredSkills = new List<string> { "React", "Next.js", "TypeScript" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Published
        },
        new Job
        {
            EmployerId = "seed-northwind", Title = "Backend Engineer (.NET)", CompanyName = "Northwind Labs",
            Description = "Own our core API and data layer.",
            EmploymentType = EmploymentType.FullTime, ExperienceLevel = ExperienceLevel.SeniorLevel,
            Location = "Remote", WorkMode = WorkMode.Remote,
            RequiredSkills = new List<string> { "C#", "EF Core", "SQL Server" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Published
        },
        new Job
        {
            EmployerId = "seed-acme", Title = "Product Designer", CompanyName = "Acme Inc",
            Description = "Shape the end-to-end experience for our job seekers.",
            EmploymentType = EmploymentType.FullTime, ExperienceLevel = ExperienceLevel.MidLevel,
            Location = "Dhaka", WorkMode = WorkMode.OnSite,
            RequiredSkills = new List<string> { "Figma", "User Research" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Published
        },
        new Job
        {
            EmployerId = "seed-northwind", Title = "QA Engineer", CompanyName = "Northwind Labs",
            Description = "Own quality across our web and API surfaces.",
            EmploymentType = EmploymentType.Contract, ExperienceLevel = ExperienceLevel.EntryLevel,
            Location = "Remote", WorkMode = WorkMode.Remote,
            RequiredSkills = new List<string> { "Manual Testing", "Cypress" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Published
        },
        new Job
        {
            EmployerId = "seed-acme", Title = "Expired Internship (should NOT appear)", CompanyName = "Acme Inc",
            Description = "Seeded already-closed to verify the eligibility filter works.",
            EmploymentType = EmploymentType.Internship, ExperienceLevel = ExperienceLevel.EntryLevel,
            Location = "Remote", WorkMode = WorkMode.Remote,
            RequiredSkills = new List<string> { "Java", "Spring Boot" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Closed
        },
        new Job
        {
            EmployerId = "seed-acme", Title = "Draft Role (should NOT appear)", CompanyName = "Acme Inc",
            Description = "Seeded as Draft to verify it never becomes publicly visible.",
            EmploymentType = EmploymentType.PartTime, ExperienceLevel = ExperienceLevel.MidLevel,
            Location = "Dhaka", WorkMode = WorkMode.Hybrid,
            RequiredSkills = new List<string> { "SQL" },
            Vacancies = 1, ExpireAt = DateTime.UtcNow.AddMonths(1), Status = JobStatus.Draft
        }
    };
}