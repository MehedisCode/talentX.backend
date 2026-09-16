using System.Text;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class GeminiPromptBuilder
{
    public static object BuildRequestBody(
        string plainText,
        IReadOnlyList<string> links,
        string? deterministicEmail = null,
        string? deterministicPhone = null)
    {
        var promptPayload = FormatPromptPayload(plainText, links, deterministicEmail, deterministicPhone);

        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = promptPayload }
                    }
                }
            },
            generationConfig = BuildGenerationConfig()
        };
    }

    private static string FormatPromptPayload(
        string plainText,
        IReadOnlyList<string> links,
        string? deterministicEmail,
        string? deterministicPhone)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildPrompt());
        sb.AppendLine();
        sb.AppendLine("RESUME TEXT:");
        sb.AppendLine(plainText);

        if (!string.IsNullOrWhiteSpace(deterministicEmail) || !string.IsNullOrWhiteSpace(deterministicPhone))
        {
            sb.AppendLine();
            sb.AppendLine("EXTRACTED CONTACT INFORMATION:");
            if (!string.IsNullOrWhiteSpace(deterministicEmail))
                sb.AppendLine($"Email: {deterministicEmail}");
            if (!string.IsNullOrWhiteSpace(deterministicPhone))
                sb.AppendLine($"Phone: {deterministicPhone}");
        }

        if (links.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ACTUAL DOCUMENT HYPERLINKS:");
            foreach (var link in links)
            {
                sb.AppendLine(link);
            }
        }

        return sb.ToString();
    }

    private static string BuildPrompt() =>
        "Extract structured resume information from the provided resume.\n" +
        "Return ONLY valid JSON matching the schema.\n\n" +
        "EXTRACTION & CLASSIFICATION GUIDELINES:\n" +
        "- fullName: Candidate's full name (usually located at the top/header of the resume).\n" +
        "- location: Candidate's current residence/location (city, state/province, country). Do NOT use company locations as candidate location.\n" +
        "- technicalSkills: Programming languages, frameworks, libraries, databases, and core technical proficiencies (e.g. C#, .NET, Python, React, PostgreSQL, REST APIs, GraphQL).\n" +
        "- toolsAndPlatforms: Developer tools, cloud providers, DevOps tools, version control, and infrastructure (e.g. Git, Docker, Kubernetes, AWS, Azure, GCP, Linux, Jira, Postman, CI/CD).\n" +
        "- softSkills: Interpersonal, leadership, communication, management, and agile/scrum methodologies (e.g. Problem Solving, Team Leadership, Agile/Scrum, Collaboration).\n" +
        "- workExperience: Extract all work history items. For current/ongoing roles, set 'endDate' to 'Present' or 'Current'. If only one date is given, populate 'startDate' and set 'endDate' to 'Present' or empty.\n" +
        "- education: Extract all degrees, universities, and colleges. When the resume shows a date range (e.g. '2016 - 2020'), put the first year in 'startDate' and the second year in 'endDate'. For ongoing studies, set 'endDate' to 'Expected <Year>' or 'Present'. If the resume lists only a SINGLE year for a degree, treat that year as the completion/graduation year and put it in 'endDate' (leave 'startDate' empty). Do not invent dates that are not present.\n\n" +
        "IMPORTANT RULES FOR CONTACT & HYPERLINKS:\n" +
        "- Do NOT guess or invent email addresses or phone numbers if none are found.\n" +
        "- The 'ACTUAL DOCUMENT HYPERLINKS' section contains authoritative URLs extracted directly from the document's link annotations and structure.\n" +
        "- Preserve the URLs EXACTLY as provided (do not alter, truncate, or rewrite them).\n" +
        "- NEVER invent, reconstruct, guess, or fabricate any URL.\n" +
        "- NEVER replace an extracted URL with a guessed URL.\n" +
        "- linkedInUrl: ONLY the candidate's LinkedIn profile URL (e.g. linkedin.com/in/username). Leave it empty if the resume has no LinkedIn URL.\n" +
        "- portfolioUrls: ONLY the candidate's OWN portfolio, personal website, personal blog, or design portfolio (e.g. Behance, Dribbble, a *.github.io site, a personal domain, or a GitHub/GitLab profile).\n" +
        "- Do NOT add any of the following to portfolioUrls: LinkedIn, social media (Facebook, X/Twitter, Instagram, TikTok, YouTube), job boards or freelance marketplaces, company or university websites, link shorteners, or email/messaging links.\n" +
        "- Do NOT treat every URL as a portfolio. When you are unsure whether a URL is the candidate's personal portfolio, OMIT it — an empty list is better than an unrelated link.\n" +
        "- Your job is strictly to extract semantic information and classify the extracted URLs into the appropriate fields.";

    private static object BuildGenerationConfig() => new
    {
        temperature = 0.0,
        top_p = 0.95,
        response_mime_type = "application/json",
        response_schema = new
        {
            type = "OBJECT",
            required = new[]
            {
                "fullName",
                "technicalSkills",
                "softSkills",
                "toolsAndPlatforms",
                "workExperience",
                "education"
            },
            properties = new
            {
                fullName = new { type = "STRING" },
                email = new { type = "STRING" },
                phone = new { type = "STRING" },
                location = new { type = "STRING" },
                linkedInUrl = new { type = "STRING" },
                portfolioUrls = new { type = "ARRAY", items = new { type = "STRING" } },
                technicalSkills = new { type = "ARRAY", items = new { type = "STRING" } },
                softSkills = new { type = "ARRAY", items = new { type = "STRING" } },
                toolsAndPlatforms = new { type = "ARRAY", items = new { type = "STRING" } },
                workExperience = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        required = new[] { "companyName", "jobTitle", "startDate" },
                        properties = new
                        {
                            companyName = new { type = "STRING" },
                            jobTitle = new { type = "STRING" },
                            startDate = new { type = "STRING" },
                            endDate = new { type = "STRING" }
                        }
                    }
                },
                education = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        required = new[] { "institution", "degree", "startDate" },
                        properties = new
                        {
                            institution = new { type = "STRING" },
                            degree = new { type = "STRING" },
                            startDate = new { type = "STRING" },
                            endDate = new { type = "STRING" }
                        }
                    }
                }
            }
        }
    };
}
