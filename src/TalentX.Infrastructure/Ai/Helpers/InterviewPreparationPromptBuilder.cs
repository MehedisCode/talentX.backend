using System.Text.Json;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class InterviewPreparationPromptBuilder
{
    public static string BuildPreparationPrompt(InterviewPreparationContext context)
    {
        return $@"
You are an expert career coach and interview preparation specialist.
Generate a comprehensive, personalized interview preparation guide.

IMPORTANT RULES:
- This is PREPARATION GUIDANCE only. Never make hiring decisions or guarantees.
- Do not expose any confidential employer information beyond what is in the job posting.
- Do not reference or compare with other candidates.
- Tailor all questions and advice to the specific candidate's background.
- If information is limited (e.g., missing job description or candidate has no listed work experience), generate the best possible preparation guidance using whatever data IS available. Focus on general best practices for the role type and industry-standard interview questions.

JOB INFORMATION:
- Title: {context.JobTitle}
- Company: {context.CompanyName}
- Description: {context.JobDescription}
- Responsibilities: {context.Responsibilities}
- Requirements: {context.Requirements}
- Required Skills: {string.Join(", ", context.RequiredSkills)}
- Experience Level: {context.ExperienceLevel}

CANDIDATE PROFILE:
- Name: {context.TalentName}
- Bio: {context.ShortBio}
- Years of Experience: {context.TotalYearsExperience}
- Technical Skills: {string.Join(", ", context.TechnicalSkills)}
- Soft Skills: {string.Join(", ", context.SoftSkills)}
- Tools & Platforms: {string.Join(", ", context.ToolsAndPlatforms)}
- Work Experience: {JsonSerializer.Serialize(context.WorkExperiences)}
- Education: {JsonSerializer.Serialize(context.Educations)}

INTERVIEW CONTEXT:
- Type: {context.InterviewType}
- Scheduled: {context.ScheduledAt}

Generate 5-7 questions per category. Each question should include a helpful tip
for the candidate on how to approach it based on THEIR specific experience.
Focus topics and revision areas on gaps between the candidate's profile
and the job requirements.
";
    }

    public static string BuildAdditionalQuestionsPrompt(AdditionalQuestionsContext context)
    {
        return $@"
You are an expert career coach and interview preparation specialist.
Generate a supplementary list of personalized interview questions for a candidate.

IMPORTANT RULES:
- This is PREPARATION GUIDANCE only. Never make hiring decisions or guarantees.
- Do not duplicate any of the previously generated questions provided below.

CONTEXT:
- Candidate Name: {context.TalentName}
- Job Title: {context.JobTitle}
- Company: {context.CompanyName}

EXISTING QUESTIONS (DO NOT DUPLICATE THESE):
- Technical: {context.ExistingTechnicalQuestions}
- Behavioral: {context.ExistingBehavioralQuestions}
- Experience-Based: {context.ExistingExperienceBasedQuestions}
- Role-Specific: {context.ExistingRoleSpecificQuestions}

Generate 5-10 completely new questions across any relevant category. 
Each question should include a helpful tip for the candidate.
";
    }

    public static object BuildPreparationSchema()
    {
        var questionItemSchema = new
        {
            type = "OBJECT",
            properties = new
            {
                question = new { type = "STRING" },
                category = new { type = "STRING" },
                tip = new { type = "STRING" }
            },
            required = new[] { "question", "category", "tip" }
        };

        return new
        {
            type = "OBJECT",
            properties = new
            {
                technicalQuestions = new { type = "ARRAY", items = questionItemSchema },
                behavioralQuestions = new { type = "ARRAY", items = questionItemSchema },
                experienceBasedQuestions = new { type = "ARRAY", items = questionItemSchema },
                roleSpecificQuestions = new { type = "ARRAY", items = questionItemSchema },
                keyTopicsToPrepare = new { type = "ARRAY", items = new { type = "STRING" } },
                suggestedRevisionAreas = new { type = "ARRAY", items = new { type = "STRING" } }
            },
            required = new[] 
            { 
                "technicalQuestions", 
                "behavioralQuestions", 
                "experienceBasedQuestions", 
                "roleSpecificQuestions", 
                "keyTopicsToPrepare", 
                "suggestedRevisionAreas" 
            }
        };
    }

    public static object BuildAdditionalQuestionsSchema()
    {
        var questionItemSchema = new
        {
            type = "OBJECT",
            properties = new
            {
                question = new { type = "STRING" },
                category = new { type = "STRING" },
                tip = new { type = "STRING" }
            },
            required = new[] { "question", "category", "tip" }
        };

        return new
        {
            type = "OBJECT",
            properties = new
            {
                additionalQuestions = new
                {
                    type = "ARRAY",
                    items = questionItemSchema
                }
            },
            required = new[] { "additionalQuestions" }
        };
    }
}
