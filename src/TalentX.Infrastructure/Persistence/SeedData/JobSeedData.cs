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
            // ACME INC (Fintech & Cloud SaaS)
            // ==========================================
            Job.Create(
                "seed-acme", "Frontend Developer", "Acme Inc",
                "Build and ship responsive user-facing features across our product suite using React 19, Next.js, and TypeScript.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "React", "Next.js", "TypeScript", "Tailwind CSS" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-acme", "Product Designer", "Acme Inc",
                "Shape the end-to-end experience for our job discovery platform, creating intuitive user flows and design systems in Figma.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Figma", "User Research", "Design Systems", "Prototyping" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-acme", "Senior Data Engineer", "Acme Inc",
                "Design and scale real-time event pipelines, ETL workflows, and lakehouse infrastructure using Apache Spark, Kafka, and dbt.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Python", "Apache Spark", "Kafka", "PostgreSQL", "dbt" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-acme", "Growth Marketing Intern", "Acme Inc",
                "Support our acquisition campaigns, content writing, SEO optimization, and social media analytics.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Content Writing", "SEO", "Google Analytics", "Social Media" },
                2, now.AddMonths(1)
            ),
            Job.Create(
                "seed-acme", "VP of Engineering", "Acme Inc",
                "Lead an engineering organization of 40+ across 4 domains. Drive architectural strategy, hiring, and operational excellence.",
                EmploymentType.FullTime, ExperienceLevel.Executive, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Engineering Leadership", "System Architecture", "Strategic Planning", "Mentorship" },
                1, now.AddMonths(4)
            ),
            Job.Create(
                "seed-acme", "Part-Time Technical Writer", "Acme Inc",
                "Write developer documentation, API references, integration tutorials, and release changelogs.",
                EmploymentType.PartTime, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Technical Writing", "Markdown", "REST APIs", "Git" },
                1, now.AddMonths(2)
            ),

            // ==========================================
            // NORTHWIND LABS (Cloud Platforms & Infrastructure)
            // ==========================================
            Job.Create(
                "seed-northwind", "Senior Backend Engineer (.NET)", "Northwind Labs",
                "Own our core high-throughput microservices using .NET 10, EF Core, PostgreSQL, and distributed caching with Redis.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Chittagong", WorkArrangement.Remote,
                new List<string> { "C#", "ASP.NET Core", "EF Core", "PostgreSQL", "Redis", "Docker" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "QA Automation Engineer", "Northwind Labs",
                "Design and maintain automated end-to-end regression suites using Playwright, Cypress, and CI/CD pipelines.",
                EmploymentType.Contract, ExperienceLevel.EntryLevel, "Chittagong", WorkArrangement.Remote,
                new List<string> { "Playwright", "Cypress", "TypeScript", "Postman", "GitHub Actions" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "Staff DevOps & Cloud Architect", "Northwind Labs",
                "Architect resilient AWS Kubernetes clusters, manage multi-region Terraform modules, and implement zero-trust security.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Remote", WorkArrangement.Remote,
                new List<string> { "AWS", "Kubernetes", "Terraform", "ArgoCD", "Prometheus", "Helm" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-northwind", "Junior React Developer", "Northwind Labs",
                "Build reusable UI component libraries, optimize web performance, and collaborate with backend developers on REST integrations.",
                EmploymentType.FullTime, ExperienceLevel.EntryLevel, "Sylhet", WorkArrangement.Hybrid,
                new List<string> { "React", "JavaScript", "HTML/CSS", "Git", "REST APIs" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-northwind", "Software Engineering Intern (.NET)", "Northwind Labs",
                "Learn enterprise backend engineering with C# and ASP.NET Core while working on real customer-facing features.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Chittagong", WorkArrangement.Remote,
                new List<string> { "C#", ".NET", "SQL", "OOP" },
                3, now.AddMonths(1)
            ),
            Job.Create(
                "seed-northwind", "Site Reliability Engineer (Contract)", "Northwind Labs",
                "Improve system uptime, establish SLOs/SLAs, conduct incident post-mortems, and automate observability dashboards.",
                EmploymentType.Contract, ExperienceLevel.SeniorLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "SRE", "Linux", "Grafana", "Datadog", "Bash", "Python" },
                1, now.AddMonths(2)
            ),

            // ==========================================
            // STELLAR SYSTEMS (AI & Machine Learning)
            // ==========================================
            Job.Create(
                "seed-stellar", "Engineering Manager (AI Platform)", "Stellar Systems",
                "Lead a world-class team of 8 ML engineers and full-stack developers building generative AI workflow solutions.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Team Leadership", "Agile", "LLM Ops", "System Design", "Roadmaps" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-stellar", "Mobile Developer (Flutter)", "Stellar Systems",
                "Build polished, offline-first mobile applications for iOS and Android with smooth 60fps animations.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Remote,
                new List<string> { "Flutter", "Dart", "Firebase", "State Management", "REST APIs" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-stellar", "Senior Machine Learning Engineer", "Stellar Systems",
                "Train, fine-tune, and deploy transformer models, RAG pipelines, and vector database search systems at scale.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Python", "PyTorch", "Transformers", "Qdrant", "FastAPI", "Docker" },
                2, now.AddMonths(3)
            ),
            Job.Create(
                "seed-stellar", "AI Research Intern", "Stellar Systems",
                "Work closely with our research scientists on multimodal document understanding and prompt optimization benchmarks.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Python", "NumPy", "Pandas", "Linear Algebra", "Machine Learning" },
                2, now.AddMonths(1)
            ),
            Job.Create(
                "seed-stellar", "Contract UI/UX Designer", "Stellar Systems",
                "Craft user experiences for our AI copilot extension, conducting usability studies and rapid prototyping.",
                EmploymentType.Contract, ExperienceLevel.MidLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Figma", "UI Design", "Wireframing", "Usability Testing" },
                1, now.AddMonths(2)
            ),

            // ==========================================
            // HORIZON CAPITAL (Fintech & Digital Banking)
            // ==========================================
            Job.Create(
                "seed-horizon", "Senior Data Scientist (Risk Analytics)", "Horizon Capital",
                "Develop credit scoring, fraud detection, and transaction anomaly models handling millions of transactions daily.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Python", "scikit-learn", "LightGBM", "SQL", "Feature Store", "Fintech" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-horizon", "Chief Technology Officer", "Horizon Capital",
                "Set the technology vision, lead security compliance (PCI-DSS/ISO 27001), and scale enterprise core banking architecture.",
                EmploymentType.FullTime, ExperienceLevel.Executive, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Executive Leadership", "Fintech Architecture", "Information Security", "Enterprise Strategy" },
                1, now.AddMonths(6)
            ),
            Job.Create(
                "seed-horizon", "Compliance Analyst (Contract)", "Horizon Capital",
                "Perform AML checks, KYC verification audits, and regulatory reporting for Bangladesh Bank compliance.",
                EmploymentType.Contract, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Regulatory Compliance", "Risk Management", "AML", "KYC", "Audit" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-horizon", "Junior Full Stack Developer", "Horizon Capital",
                "Develop internal admin portals, reporting dashboards, and loan management tools using Next.js and C# backend.",
                EmploymentType.FullTime, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "TypeScript", "React", "C#", "SQL Server", "HTML/CSS" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-horizon", "Part-Time Financial Analyst", "Horizon Capital",
                "Analyze market trends, create financial model spreadsheets, and assist in investor pitch deck preparations.",
                EmploymentType.PartTime, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Financial Modeling", "Excel", "PowerBI", "Market Research" },
                1, now.AddMonths(1)
            ),

            // ==========================================
            // GREENBRIDGE NGO (Digital Public Goods & Social Impact)
            // ==========================================
            Job.Create(
                "seed-greenbridge", "Full Stack Developer", "GreenBridge NGO",
                "Maintain and scale our national volunteer management and emergency relief dispatch platform.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Khulna", WorkArrangement.Remote,
                new List<string> { "Node.js", "React", "PostgreSQL", "Docker", "Express" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-greenbridge", "Project Coordinator (Part-Time)", "GreenBridge NGO",
                "Coordinate regional field volunteer operations, donor milestone updates, and event logistics across Rajshahi and Khulna.",
                EmploymentType.PartTime, ExperienceLevel.EntryLevel, "Rajshahi", WorkArrangement.OnSite,
                new List<string> { "Project Management", "Communication", "Event Planning", "Reporting" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-greenbridge", "Lead Software Architect", "GreenBridge NGO",
                "Architect open-source health and disaster response systems designed for low-bandwidth rural environments.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Remote", WorkArrangement.Remote,
                new List<string> { "System Architecture", "Microservices", "Offline-First", "PostgreSQL", "Security" },
                1, now.AddMonths(4)
            ),
            Job.Create(
                "seed-greenbridge", "Community Outreach Intern", "GreenBridge NGO",
                "Engage with university youth clubs, conduct survey outreach, and document community impact stories.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Khulna", WorkArrangement.OnSite,
                new List<string> { "Outreach", "Storytelling", "Survey Data", "Social Media" },
                3, now.AddMonths(1)
            ),

            // ==========================================
            // APEX CYBER (Cybersecurity & DevSecOps)
            // ==========================================
            Job.Create(
                "seed-apex", "Security Engineer (AppSec)", "Apex Cyber",
                "Conduct code audits, SAST/DAST pipeline integration, threat modeling, and vulnerability remediation across web APIs.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Application Security", "OWASP", "SAST", "Penetration Testing", "Python" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-apex", "Junior SOC Analyst", "Apex Cyber",
                "Monitor SIEM alerts, investigate suspicious login anomalies, and assist in incident triage.",
                EmploymentType.FullTime, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "SIEM", "Wireshark", "Network Security", "Linux", "Incident Response" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-apex", "Lead Penetration Tester (Contract)", "Apex Cyber",
                "Execute red-team assessments, cloud environment pen-testing (AWS/Azure), and deliver executive remediation reports.",
                EmploymentType.Contract, ExperienceLevel.Lead, "Remote", WorkArrangement.Remote,
                new List<string> { "Penetration Testing", "Burp Suite", "Metasploit", "Cloud Security", "OSCP" },
                1, now.AddMonths(2)
            ),

            // ==========================================
            // NEXUS COMMERCE (E-Commerce & Logistics)
            // ==========================================
            Job.Create(
                "seed-nexus", "Senior React Native Developer", "Nexus Commerce",
                "Build customer-facing courier tracking, merchant payment checkout, and rider app features with React Native.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "React Native", "TypeScript", "Redux Toolkit", "Maps API", "WebSocket" },
                2, now.AddMonths(3)
            ),
            Job.Create(
                "seed-nexus", "Warehouse Operations Manager", "Nexus Commerce",
                "Oversee inventory logistics, fulfillment center workflows, and warehouse management system operations.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Chittagong", WorkArrangement.OnSite,
                new List<string> { "Supply Chain", "Logistics", "Operations", "Inventory Control" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-nexus", "Part-Time Customer Support Lead", "Nexus Commerce",
                "Manage customer escalation tickets, handle merchant live chat support, and supervise junior helpdesk agents.",
                EmploymentType.PartTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.Remote,
                new List<string> { "Zendesk", "Customer Service", "Conflict Resolution", "CRM" },
                2, now.AddMonths(2)
            ),
            Job.Create(
                "seed-nexus", "E-Commerce QA Tester", "Nexus Commerce",
                "Test shopping cart flows, payment gateway callbacks, checkout promo codes, and multi-device cross-browser compatibility.",
                EmploymentType.Contract, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Manual Testing", "Regression Testing", "Jira", "Payment Testing" },
                2, now.AddMonths(1)
            ),

            // ==========================================
            // HYPERION HEALTH (HealthTech & Telemedicine)
            // ==========================================
            Job.Create(
                "seed-hyperion", "Senior Full Stack Engineer (Node + Next.js)", "Hyperion Health",
                "Build doctor video consultation portals, electronic health record (EHR) managers, and prescription dispatchers.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Sylhet", WorkArrangement.Remote,
                new List<string> { "Next.js", "Node.js", "WebRTC", "PostgreSQL", "HIPAA" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-hyperion", "Health Informatics Intern", "Hyperion Health",
                "Clean clinical survey data, categorize medical terminology codes (ICD-10), and assist healthcare data scientists.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Sylhet", WorkArrangement.OnSite,
                new List<string> { "Data Cleaning", "Excel", "Medical Informatics", "Healthcare" },
                2, now.AddMonths(1)
            ),
            Job.Create(
                "seed-hyperion", "Product Manager (Telehealth)", "Hyperion Health",
                "Define the patient experience roadmap, conduct clinician interviews, and measure telemedicine adoption KPIs.",
                EmploymentType.FullTime, ExperienceLevel.Lead, "Dhaka", WorkArrangement.Hybrid,
                new List<string> { "Product Management", "Telemedicine", "User Stories", "Analytics", "Roadmaps" },
                1, now.AddMonths(3)
            ),

            // ==========================================
            // QUANTUM LEAP AI (Robotics & Computer Vision)
            // ==========================================
            Job.Create(
                "seed-quantum", "Computer Vision Research Engineer", "Quantum Leap AI",
                "Develop real-time object detection and segmentation algorithms for automated industrial quality inspection.",
                EmploymentType.FullTime, ExperienceLevel.SeniorLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "Python", "OpenCV", "YOLO", "C++", "TensorRT", "CUDA" },
                1, now.AddMonths(3)
            ),
            Job.Create(
                "seed-quantum", "Embedded Systems Developer", "Quantum Leap AI",
                "Program microcontroller firmware (STM32/ESP32), sensor fusion buses (I2C/SPI/CAN), and edge inference controllers.",
                EmploymentType.FullTime, ExperienceLevel.MidLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "C", "C++", "Embedded Systems", "FreeRTOS", "Hardware Debugging" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-quantum", "Contract Technical Recruiter", "Quantum Leap AI",
                "Source and screen niche hardware, AI, and robotics engineers across international networks.",
                EmploymentType.Contract, ExperienceLevel.MidLevel, "Remote", WorkArrangement.Remote,
                new List<string> { "Technical Sourcing", "Talent Acquisition", "LinkedIn Recruiter", "Interviewing" },
                1, now.AddMonths(2)
            ),
            Job.Create(
                "seed-quantum", "Robotics Software Intern", "Quantum Leap AI",
                "Simulate robot arm kinematics in ROS2 and Gazebo, and assist senior roboticists in physical test runs.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Dhaka", WorkArrangement.OnSite,
                new List<string> { "ROS2", "Python", "Linux", "Robotics", "C++" },
                2, now.AddMonths(1)
            ),

            // ==========================================
            // ELIGIBILITY TEST ROLES (Expired / Draft)
            // ==========================================
            // Intentionally expired — must NEVER appear in public search results
            Job.Create(
                "seed-acme", "Expired Java Spring Intern (should NOT appear)", "Acme Inc",
                "Seeded already-expired to verify the ExpireAt filter works accurately.",
                EmploymentType.Internship, ExperienceLevel.EntryLevel, "Remote", WorkArrangement.Remote,
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
