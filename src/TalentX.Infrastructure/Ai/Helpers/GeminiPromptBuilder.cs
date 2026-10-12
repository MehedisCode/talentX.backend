using System.Text;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class GeminiPromptBuilder
{
    public static object BuildRequestBody(
        string plainText,
        IReadOnlyList<string> links,
        ResumeExtractionPurpose purpose)
    {
        return BuildRequestBody(
            plainText,
            links,
            new GeminiPromptBuildOptions(Purpose: purpose));
    }

    public static object BuildRequestBody(
        string plainText,
        IReadOnlyList<string> links,
        GeminiPromptBuildOptions? options = null)
    {
        var promptOptions = options ?? new GeminiPromptBuildOptions();

        var promptPayload = FormatPromptPayload(
            plainText,
            links,
            promptOptions);

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
            generationConfig = BuildGenerationConfig(promptOptions.MaxOutputTokens, promptOptions.Purpose)
        };
    }

    private static string FormatPromptPayload(
        string plainText,
        IReadOnlyList<string> links,
        GeminiPromptBuildOptions options)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(GetPrompt(options.Purpose));
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("RESUME TEXT:");
        stringBuilder.AppendLine(plainText);

        if (!string.IsNullOrWhiteSpace(options.DeterministicEmail) || !string.IsNullOrWhiteSpace(options.DeterministicPhone))
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("EXTRACTED CONTACT INFORMATION:");
            if (!string.IsNullOrWhiteSpace(options.DeterministicEmail))
            {
                stringBuilder.AppendLine($"Email: {options.DeterministicEmail}");
            }
            if (!string.IsNullOrWhiteSpace(options.DeterministicPhone))
            {
                stringBuilder.AppendLine($"Phone: {options.DeterministicPhone}");
            }
        }

        if (links.Count > 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("ACTUAL DOCUMENT HYPERLINKS:");
            foreach (var link in links)
            {
                stringBuilder.AppendLine(link);
            }
        }

        return stringBuilder.ToString();
    }

    private const string ResumeExtractionPrompt =
        """
        Extract structured resume information from the provided resume.
        Return ONLY valid JSON matching the schema.

        EXTRACTION & CLASSIFICATION GUIDELINES:
        - fullName: Candidate's full name (usually located at the top/header of the resume).
        - tagline: Headline / professional title (e.g. 'Senior Full Stack Engineer'). Null if not found.
        - shortBio: Executive summary / about me / professional summary. Null if not found.
        - location: Candidate's current residence/location (city, state/province, country). Do NOT use company locations as candidate location.
        - totalYearsExperience: Total years of professional experience as a number (e.g. 5, 8.5). Null if not found.
        - expectedSalary: Desired/expected salary or compensation if explicitly mentioned. Null if not found.
        - workExperience: Extract EVERY work experience entry in the resume — full-time, part-time, internship, contract, and freelance roles. Do not merge or skip roles, and do not omit a role just because its dates are missing (leave the date fields empty instead). Only treat a role as CURRENT (set 'endDate' to 'Present' and 'isCurrent' to true) when the resume indicates it: a start date with no end date, or wording like Present / Current / till date. A range such as '(December 2024 - Present)' means startDate 'December 2024' and endDate 'Present' with 'isCurrent' true. If the resume gives NO dates for a role, leave both date fields empty and set 'isCurrent' to false — do not infer or invent 'Present'. For past roles, set 'isCurrent' to false and fill 'startDate' and 'endDate' from the resume.
        - education: Extract all degrees, universities, and colleges. When the resume shows a date range (e.g. '2016 - 2020'), put the first year in 'startDate' and the second year in 'endDate'. For ongoing studies, set 'endDate' to 'Expected <Year>' or 'Present'. If the resume lists only a SINGLE year for a degree, treat that year as the completion/graduation year and put it in 'endDate' (leave 'startDate' empty). Do not invent dates that are not present.
        - projects: Extract notable projects into the projects field: array of { projectName, role, projectType ("Professional"|"Personal"|"Academic"), startDate, endDate, isOngoing, description, technologiesUsed (array of string tags), projectUrl }.
        - technicalSkills: Programming languages, frameworks, libraries, databases, and core technical proficiencies (e.g. C#, .NET, Python, React, PostgreSQL, REST APIs, GraphQL).
        - toolsAndPlatforms: Developer tools, cloud providers, DevOps tools, version control, and infrastructure (e.g. Git, Docker, Kubernetes, AWS, Azure, GCP, Linux, Jira, Postman, CI/CD).
        - softSkills: Interpersonal, leadership, communication, management, and agile/scrum methodologies (e.g. Problem Solving, Team Leadership, Agile/Scrum, Collaboration).
        - skillPropositions: Key strengths, value propositions, or summary highlights as a list of strings. Empty list if none.

        IMPORTANT RULES FOR CONTACT & HYPERLINKS:
        - Do NOT guess or invent email addresses or phone numbers if none are found.
        - The 'ACTUAL DOCUMENT HYPERLINKS' section contains authoritative URLs extracted directly from the document's link annotations and structure.
        - Preserve the URLs EXACTLY as provided (do not alter, truncate, or rewrite them) — the only exception is adding an 'https://' prefix when a URL is written without a scheme.
        - Candidate URLs may appear either in the 'ACTUAL DOCUMENT HYPERLINKS' list or as plain text in the resume (e.g. a personal portfolio domain such as 'aparnakhatri.com'). If a link is written without a scheme, output it with an 'https://' prefix; do not invent or change the host name.
        - NEVER invent, reconstruct, guess, or fabricate any URL.
        - NEVER replace an extracted URL with a guessed URL.
        - linkedInUrl: ONLY the candidate's LinkedIn profile URL (e.g. linkedin.com/in/username). Leave it empty if the resume has no LinkedIn URL.
        - portfolioUrls: ONLY the candidate's OWN portfolio, personal website, personal blog, or design portfolio (e.g. Behance, Dribbble, a *.github.io site, a personal domain, or a GitHub/GitLab profile).
        - Do NOT add any of the following to portfolioUrls: LinkedIn, social media (Facebook, X/Twitter, Instagram, TikTok, YouTube), job boards or freelance marketplaces, company or university websites, link shorteners, or email/messaging links.
        - Do NOT treat every URL as a portfolio. When you are unsure whether a URL is the candidate's personal portfolio, OMIT it — an empty list is better than an unrelated link.
        - If the resume contains multiple LinkedIn URLs, extract ONLY the primary one (usually the one in the header or contact section).
        - If information appears redacted or masked (e.g. 'Confidential', 'XXX', '[REDACTED]'), leave the field empty rather than guessing.
        - Do not extract invalid or impossible dates (e.g. month > 12, day > 31, February 30th).
        - Your job is strictly to extract semantic information and classify the extracted URLs into the appropriate fields.
        """;

    private static string GetPrompt(ResumeExtractionPurpose purpose = ResumeExtractionPurpose.JobApplication) =>
        ResumeExtractionPrompt;

    private static object BuildGenerationConfig(int maxOutputTokens, ResumeExtractionPurpose purpose) => new
    {
        temperature = 0.0,
        top_p = 0.95,
        max_output_tokens = maxOutputTokens,
        response_mime_type = "application/json",
        responseJsonSchema = new
        {
            type = "object",
            required = new[]
            {
                "fullName",
                "workExperience",
                "education",
                "technicalSkills",
                "softSkills",
                "toolsAndPlatforms"
            },
            properties = BuildProperties(purpose)
        }
    };

    private static object BuildProperties(ResumeExtractionPurpose purpose = ResumeExtractionPurpose.JobApplication)
    {
        var workExperience = new
        {
            type = "array",
            items = new
            {
                type = "object",
                required = new[] { "companyName", "jobTitle" },
                properties = new
                {
                    companyName = new { type = "string" },
                    jobTitle = new { type = "string" },
                    startDate = new { type = "string" },
                    endDate = new { type = "string" },
                    isCurrent = new { type = "boolean" }
                }
            }
        };

        var education = new
        {
            type = "array",
            items = new
            {
                type = "object",
                required = new[] { "institution", "degree" },
                properties = new
                {
                    institution = new { type = "string" },
                    degree = new { type = "string" },
                    startDate = new { type = "string" },
                    endDate = new { type = "string" }
                }
            }
        };

        var projects = new
        {
            type = "array",
            items = new
            {
                type = "object",
                required = new[] { "projectName" },
                properties = new
                {
                    projectName = new { type = "string" },
                    role = new { type = "string" },
                    projectType = new { type = "string", description = "Professional, Personal, or Academic" },
                    startDate = new { type = "string" },
                    endDate = new { type = "string" },
                    isOngoing = new { type = "boolean" },
                    description = new { type = "string" },
                    technologiesUsed = new { type = "array", items = new { type = "string" } },
                    projectUrl = new { type = "string" }
                }
            }
        };

        var technicalSkills = new { type = "array", items = new { type = "string" } };
        var softSkills = new { type = "array", items = new { type = "string" } };
        var toolsAndPlatforms = new { type = "array", items = new { type = "string" } };
        var portfolioUrls = new { type = "array", items = new { type = "string" } };

        return new
        {
            fullName = new { type = "string" },
            tagline = new { type = "string", description = "Professional title or headline" },
            shortBio = new { type = "string", description = "Executive summary or professional bio" },
            email = new { type = "string" },
            phone = new { type = "string" },
            location = new { type = "string" },
            totalYearsExperience = new { type = "number", description = "Total years of professional experience" },
            expectedSalary = new { type = "number", description = "Expected salary or compensation if stated" },
            linkedInUrl = new { type = "string" },
            portfolioUrls,
            workExperience,
            education,
            projects,
            technicalSkills,
            softSkills,
            toolsAndPlatforms,
            skillPropositions = new { type = "array", items = new { type = "string" }, description = "Key value propositions or skill highlights" }
        };
    }
}
