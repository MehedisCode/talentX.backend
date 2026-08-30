using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Persistence.SeedData;

public static class JobSeedData
{
    public static List<Job> GetJobs()
    {
        var jobs = new List<Job>
        {
            // --- Acme Inc ---
            Job.Create(
                "seed-acme", "Frontend Developer", "Acme Inc",
                "Build and ship user-facing features across our product suite using React and Next.js.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "React", "Next.js", "TypeScript", "Tailwind CSS" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-acme", "Product Designer", "Acme Inc",
                "Shape the end-to-end experience for our job seekers and employers.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Figma", "User Research", "Prototyping" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-acme", "Data Engineer", "Acme Inc",
                "Build and maintain data pipelines that power our analytics and ML features.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Python", "Apache Spark", "SQL", "dbt" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-acme", "Marketing Intern", "Acme Inc",
                "Support our growth team with content creation, social media, and campaign analysis.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Content Writing", "Social Media", "Google Analytics" },
                2, DateTime.UtcNow.AddMonths(1)
            ),

            // --- Northwind Labs ---
            Job.Create(
                "seed-northwind", "Backend Engineer (.NET)", "Northwind Labs",
                "Own our core API and data layer. You will drive performance and reliability improvements.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Chittagong", WorkArrangement.Remote,
                new List<string> { "C#", "ASP.NET Core", "EF Core", "SQL Server" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "QA Engineer", "Northwind Labs",
                "Own quality across our web and API surfaces. Write automated test suites and own the CI pipeline.",
                EmploymentType.Contract, ExperienceLevel.EntryLevel, "Chittagong", WorkArrangement.Remote,
                new List<string> { "Manual Testing", "Cypress", "Postman" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "DevOps Engineer", "Northwind Labs",
                "Manage our cloud infrastructure on AWS and improve our CI/CD pipelines.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "AWS", "Docker", "Kubernetes", "Terraform", "GitHub Actions" },
                1, DateTime.UtcNow.AddMonths(3)
            ),
            Job.Create(
                "seed-northwind", "Junior React Developer", "Northwind Labs",
                "Join our frontend team and build delightful UI components for our SaaS platform.",
                EmploymentType.FullTime, ExperienceLevel.EntryLevel, "Sylhet", WorkArrangement.Hybrid,
                new List<string> { "React", "JavaScript", "CSS" },
                2, DateTime.UtcNow.AddMonths(2)
            ),

            // --- Stellar Systems ---
            Job.Create(
                "seed-stellar", "Engineering Manager", "Stellar Systems",
                "Lead a team of 6 engineers. Drive roadmap execution, hiring, and technical strategy.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Team Leadership", "Agile", "System Design", "Roadmap Planning" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-stellar", "Mobile Developer (Flutter)", "Stellar Systems",
                "Build cross-platform mobile applications for iOS and Android using Flutter.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Remote,
                new List<string> { "Flutter", "Dart", "Firebase", "REST APIs" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-stellar", "Part-Time Content Writer", "Stellar Systems",
                "Create technical blog posts and documentation for our developer platform.",
                EmploymentType.PartTime, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Technical Writing", "Markdown", "SEO" },
                1, DateTime.UtcNow.AddMonths(1)
            ),

            // --- Horizon Capital ---
            Job.Create(
                "seed-horizon", "Senior Data Scientist", "Horizon Capital",
                "Build predictive models for financial risk assessment and portfolio optimization.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Python", "scikit-learn", "TensorFlow", "SQL", "Statistics" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-horizon", "Chief Technology Officer", "Horizon Capital",
                "Define and execute the technology vision for a fast-growing fintech company.",
                EmploymentType.FullTime, ExperienceLevel.Executive, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Technical Strategy", "Team Building", "Architecture", "Fintech" },
                1, DateTime.UtcNow.AddMonths(3)
            ),
            Job.Create(
                "seed-horizon", "Compliance Analyst (Contract)", "Horizon Capital",
                "Conduct compliance reviews and ensure regulatory adherence across our operations.",
                EmploymentType.Contract, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Regulatory Compliance", "Risk Management", "AML", "Documentation" },
                2, DateTime.UtcNow.AddMonths(1)
            ),

            // --- GreenBridge NGO ---
            Job.Create(
                "seed-greenbridge", "Full Stack Developer", "GreenBridge NGO",
                "Build and maintain our volunteer management platform used across Bangladesh.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Khulna", WorkArrangement.Remote,
                new List<string> { "Node.js", "React", "PostgreSQL", "Docker" },
                1, DateTime.UtcNow.AddMonths(2)
            ),
            Job.Create(
                "seed-greenbridge", "Project Coordinator (Part-Time)", "GreenBridge NGO",
                "Coordinate field activities, volunteer schedules, and donor reporting.",
                EmploymentType.PartTime, ExperienceLevel.EntryLevel, "Rajshahi", WorkArrangement.OnSite,
                new List<string> { "Project Management", "Communication", "MS Office" },
                2, DateTime.UtcNow.AddMonths(1)
            ),
            Job.Create(
                "seed-greenbridge", "Lead Software Architect", "GreenBridge NGO",
                "Design the long-term technical architecture for our digital platform ecosystem.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Remote", WorkArrangement.Remote,
                new List<string> { "System Architecture", "Cloud", "Microservices", "Technical Leadership" },
                1, DateTime.UtcNow.AddMonths(3)
            ),

            // Intentionally expired — to verify eligibility filter excludes by ExpireAt
            Job.Create(
                "seed-acme", "Expired Internship (should NOT appear)", "Acme Inc",
                "Seeded already-expired to verify the eligibility filter works.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Java", "Spring Boot" },
                1, DateTime.UtcNow.AddDays(-1)
            ),

            // Intentionally left as Draft — to verify it never becomes publicly visible
            Job.Create(
                "seed-acme", "Draft Role (should NOT appear)", "Acme Inc",
                "Seeded as Draft to verify it never becomes publicly visible.",
                EmploymentType.PartTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "SQL" },
                1, DateTime.UtcNow.AddMonths(1)
            ),
        };

        // Publish all except the last two (expired one is published to test ExpireAt filter;
        // the true Draft stays as Draft to test Status filter)
        for (var i = 0; i < jobs.Count - 1; i++)
        {
            jobs[i].MarkAsPublished();
        }

        return jobs;
    }
}
