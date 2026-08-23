using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs() => new()
    {
        new Job(
            title: "Frontend Developer", companyName: "Acme Inc",
            description: "Build and ship user-facing features across our product.",
            employmentType: EmploymentType.FullTime, location: "Dhaka", workMode: WorkMode.Hybrid,
            experienceLevel: ExperienceLevel.MidLevel,
            requiredSkills: new List<string> { "React", "Next.js", "TypeScript" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-acme", status: JobStatus.Published
        ),
        new Job(
            title: "Backend Engineer (.NET)", companyName: "Northwind Labs",
            description: "Own our core API and data layer.",
            employmentType: EmploymentType.FullTime, location: "Remote", workMode: WorkMode.Remote,
            experienceLevel: ExperienceLevel.SeniorLevel,
            requiredSkills: new List<string> { "C#", "EF Core", "SQL Server" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-northwind", status: JobStatus.Published
        ),
        new Job(
            title: "Product Designer", companyName: "Acme Inc",
            description: "Shape the end-to-end experience for our job seekers.",
            employmentType: EmploymentType.FullTime, location: "Dhaka", workMode: WorkMode.OnSite,
            experienceLevel: ExperienceLevel.MidLevel,
            requiredSkills: new List<string> { "Figma", "User Research" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-acme", status: JobStatus.Published
        ),
        new Job(
            title: "QA Engineer", companyName: "Northwind Labs",
            description: "Own quality across our web and API surfaces.",
            employmentType: EmploymentType.Contract, location: "Remote", workMode: WorkMode.Remote,
            experienceLevel: ExperienceLevel.EntryLevel,
            requiredSkills: new List<string> { "Manual Testing", "Cypress" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-northwind", status: JobStatus.Published
        ),
        new Job(
            title: "Expired Internship (should NOT appear)", companyName: "Acme Inc",
            description: "Seeded already-closed to verify the eligibility filter works.",
            employmentType: EmploymentType.Internship, location: "Remote", workMode: WorkMode.Remote,
            experienceLevel: ExperienceLevel.EntryLevel,
            requiredSkills: new List<string> { "Java", "Spring Boot" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-acme", status: JobStatus.Closed
        ),
        new Job(
            title: "Draft Role (should NOT appear)", companyName: "Acme Inc",
            description: "Seeded as Draft to verify it never becomes publicly visible.",
            employmentType: EmploymentType.PartTime, location: "Dhaka", workMode: WorkMode.Hybrid,
            experienceLevel: ExperienceLevel.MidLevel,
            requiredSkills: new List<string> { "SQL" },
            vacancies: 1, expireAt: DateTime.UtcNow.AddMonths(1), employerId: "seed-acme", status: JobStatus.Draft
        )
    };
}