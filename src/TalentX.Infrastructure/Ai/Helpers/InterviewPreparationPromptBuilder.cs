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

    public static string BuildEvaluationPrompt(AnswersEvaluationContext context)
    {
        var answeredQuestions = string.Join("\n", context.AnsweredQuestions.Select(a =>
            $@"[{a.Category}] (index {a.QuestionIndex}) {a.Question}
Answer: {a.Answer}"));

        return $@"
You are an expert interview coach. Evaluate the candidate's practice answers to interview questions and produce a formative evaluation report.

IMPORTANT RULES:
- This is FORMATIVE FEEDBACK for practice only. Never make hiring decisions or guarantees.
- Judge ONLY the answer content provided below, not the candidate's profile or other candidates.
- Be constructive and specific; generic praise or generic criticism is not useful.
- Evaluate only the answered questions listed. Do not invent questions or answers.

JOB:
- Title: {context.JobTitle}
- Company: {context.CompanyName}
- Interview Type: {context.InterviewType}

CANDIDATE: {context.TalentName}

ANSWERED QUESTIONS ({context.AnsweredQuestions.Count} of {context.TotalQuestionCount} total):
{answeredQuestions}

SCORING RUBRIC (overallScore 0-100):
- Relevance (40): directly addresses what the question asks.
- Structure (25): clear organization; behavioral answers should follow STAR (Situation, Task, Action, Result) where applicable.
- Specificity (20): concrete examples, numbers, names of tools or outcomes instead of vague claims.
- Depth (15): shows reasoning, trade-offs, or learning rather than surface-level statements.

Return a JSON object with this exact structure:
{{
  ""overallScore"": 0,
  ""summary"": ""2-3 sentence overall assessment of the candidate's answers"",
  ""strengths"": [""strength1"", ""strength2""],
  ""areasToImprove"": [""improvement1"", ""improvement2""],
  ""questionFeedback"": [
    {{
      ""category"": ""..."",
      ""questionIndex"": 0,
      ""score"": 0,
      ""feedback"": ""1-2 sentences on what worked and what to change"",
      ""suggestedAnswer"": ""short improved answer sketch, or an empty string if the answer is already strong""
    }}
  ]
}}

Rules for questionFeedback:
- Include exactly one entry for every answered question above, keeping the same ""category"" and ""questionIndex"".
- Do not include entries for unanswered questions.
- Keep each feedback entry concise (max ~60 words).
- scores must be integers from 0 to 100; overallScore should reflect the weighted quality of the answers.
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

    // Mirrors the JSON structure demanded by BuildEvaluationPrompt so Gemini is
    // schema-constrained, not just asked nicely. Server-side validation still
    // verifies coverage and ranges because a schema alone proves nothing.
    public static object BuildEvaluationSchema()
    {
        var feedbackItemSchema = new
        {
            type = "OBJECT",
            properties = new
            {
                category = new { type = "STRING" },
                questionIndex = new { type = "INTEGER", minimum = 0 },
                score = new { type = "INTEGER", minimum = 0, maximum = 100 },
                feedback = new { type = "STRING" },
                suggestedAnswer = new { type = "STRING" }
            },
            required = new[] { "category", "questionIndex", "score", "feedback" }
        };

        return new
        {
            type = "OBJECT",
            properties = new
            {
                overallScore = new { type = "INTEGER", minimum = 0, maximum = 100 },
                summary = new { type = "STRING" },
                strengths = new { type = "ARRAY", items = new { type = "STRING" } },
                areasToImprove = new { type = "ARRAY", items = new { type = "STRING" } },
                questionFeedback = new { type = "ARRAY", items = feedbackItemSchema }
            },
            required = new[]
            {
                "overallScore",
                "summary",
                "strengths",
                "areasToImprove",
                "questionFeedback"
            }
        };
    }
}
