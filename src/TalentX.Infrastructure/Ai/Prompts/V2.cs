using System.Text.Json;
using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Infrastructure.Ai.Prompts;

public sealed class V2 : ITalentMatchingPrompt
{
    public string Version => "v2";

    public string BuildPrompt(TalentMatchingRequest request)
    {
        var jobJson = JsonSerializer.Serialize(request.Job, new JsonSerializerOptions { WriteIndented = true });
        var candidatesJson = JsonSerializer.Serialize(request.Candidates, new JsonSerializerOptions { WriteIndented = true });

        return $$"""
You are an expert AI recruiting and candidate matching engine.
Your task is to analyze each candidate profile against a specific job posting and calculate an ABSOLUTE compatibility match score (0 to 100) along with a concise explanation for each candidate.

ABSOLUTE SCORING RULES (CRITICAL):
- Score EVERY candidate INDEPENDENTLY against the job posting only.
- NEVER compare, rank, curve, or normalize scores against the other candidates in this request.
- The scores you return must be identical no matter which other candidates are listed alongside a given candidate, and no matter how many candidates are in the request.
- A perfect match between a candidate and the job posting must score 100, regardless of any other candidate present. A complete mismatch scores 0.
- Do not re-distribute scores, avoid score clustering, or force any particular score distribution. Pick the score each candidate earns on their own merits.

CRITICAL MATCHING CRITERIA TO EVALUATE:
1. Skills & Technologies: Compare job's RequiredSkills with candidate's TechnicalSkills, ToolsAndPlatforms, and SkillPropositions.
2. Experience Level & Years: Compare job's ExperienceLevel with candidate's TotalYearsExperience.
3. Job Title & Past Roles: Compare job title and description with candidate's past WorkExperience job titles and companies.
4. Industry Experience: Consider relevance of past companies and roles where available.
5. Location & Work Arrangement: Compare job's Location, City, Country, and WorkArrangement (Remote, Hybrid, OnSite) with candidate's Location. Remote jobs are location-flexible.
6. Education Level: Consider candidate's degrees and fields of study from Educations where available.
7. Salary Expectation: Consider candidate's ExpectedSalary alignment where available.

JOB DETAILS:
{{jobJson}}

CANDIDATES:
{{candidatesJson}}

RESPONSE INSTRUCTIONS:
Return ONLY valid JSON with no preamble or markdown formatting, following this exact schema:
{
  "matches": [
    {
      "talentUserId": "the candidate's talentUserId",
      "score": 85.5,
      "explanation": "Clear, concise 1-2 sentence justification explaining why this candidate matches the role based on skills, experience, and criteria."
    }
  ]
}
Return exactly one "matches" entry per candidate listed above, keyed by that candidate's talentUserId.
""";
    }
}
