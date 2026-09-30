using FluentAssertions;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class InterviewPreparationPromptBuilderTests
{
    private static InterviewPreparationContext BuildContext()
    {
        return new InterviewPreparationContext
        {
            JobTitle = "Backend Engineer",
            CompanyName = "Acme",
            JobDescription = "Build scalable distributed systems.",
            Responsibilities = "Design APIs and own services.",
            Requirements = "5+ years of C#.",
            RequiredSkills = new List<string> { "C#", ".NET" },
            ExperienceLevel = "MidLevel",
            EmploymentType = "FullTime",
            WorkArrangement = "FullRemote",
            TalentName = "Ada Lovelace",
            Tagline = "Distributed systems engineer",
            ShortBio = "I build reliable backends.",
            TotalYearsExperience = 6,
            TechnicalSkills = new List<string> { "C#", "Kafka" },
            SoftSkills = new List<string> { "Communication" },
            ToolsAndPlatforms = new List<string> { "Docker" },
            WorkExperiences = new List<WorkExperienceSnapshot>
            {
                new("Globex", "Backend Engineer", "2020", null, true),
            },
            Educations = new List<EducationSnapshot>
            {
                new("MIT", "BSc Computer Science", "2014", "2018"),
            },
            InterviewType = "OnCall",
            ScheduledAt = new DateTime(2026, 12, 24, 18, 30, 0, DateTimeKind.Utc),
        };
    }

    private static AdditionalQuestionsContext BuildAdditionalContext()
    {
        return new AdditionalQuestionsContext
        {
            JobTitle = "Backend Engineer",
            CompanyName = "Acme",
            TalentName = "Ada Lovelace",
            ExistingTechnicalQuestions = """[{"question":"What is a closure?"}]""",
            ExistingBehavioralQuestions = """[{"question":"Tell me about a conflict."}]""",
            ExistingExperienceBasedQuestions = "[]",
            ExistingRoleSpecificQuestions = """[{"question":"How do you scale a queue?"}]""",
        };
    }

    [Fact]
    public void BuildPreparationPrompt_IncludesJobInformation()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(BuildContext());

        prompt.Should().Contain("- Title: Backend Engineer");
        prompt.Should().Contain("- Company: Acme");
        prompt.Should().Contain("- Description: Build scalable distributed systems.");
        prompt.Should().Contain("- Responsibilities: Design APIs and own services.");
        prompt.Should().Contain("- Requirements: 5+ years of C#.");
        prompt.Should().Contain("- Required Skills: C#, .NET");
        prompt.Should().Contain("- Experience Level: MidLevel");
    }

    [Fact]
    public void BuildPreparationPrompt_IncludesCandidateProfileAndSerializedHistory()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(BuildContext());

        prompt.Should().Contain("- Name: Ada Lovelace");
        prompt.Should().Contain("- Bio: I build reliable backends.");
        prompt.Should().Contain("- Years of Experience: 6");
        prompt.Should().Contain("- Technical Skills: C#, Kafka");
        prompt.Should().Contain("- Soft Skills: Communication");
        prompt.Should().Contain("- Tools & Platforms: Docker");
        prompt.Should().Contain("Globex");
        prompt.Should().Contain("MIT");
    }

    [Fact]
    public void BuildPreparationPrompt_IncludesInterviewContext()
    {
        var context = BuildContext();
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(context);

        prompt.Should().Contain("- Type: OnCall");
        prompt.Should().Contain($"- Scheduled: {context.ScheduledAt}");
    }

    [Fact]
    public void BuildPreparationSchema_DefinesAllRequiredQuestionCategoriesAndTopics()
    {
        var schema = InterviewPreparationPromptBuilder.BuildPreparationSchema();
        var json = System.Text.Json.JsonSerializer.Serialize(schema);

        json.Should().Contain("technicalQuestions");
        json.Should().Contain("behavioralQuestions");
        json.Should().Contain("experienceBasedQuestions");
        json.Should().Contain("roleSpecificQuestions");
        json.Should().Contain("keyTopicsToPrepare");
        json.Should().Contain("suggestedRevisionAreas");
    }

    [Fact]
    public void BuildPreparationPrompt_KeepsGuidanceGuardrails()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(BuildContext());

        prompt.Should().Contain("PREPARATION GUIDANCE only");
        prompt.Should().Contain("Never make hiring decisions or guarantees");
        prompt.Should().Contain("Do not reference or compare with other candidates");
        prompt.Should().Contain("Do not expose any confidential employer information");
    }

    [Fact]
    public void BuildPreparationPrompt_SpecifiesQuestionVolumeAndTipExpectations()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(BuildContext());

        prompt.Should().Contain("Generate 5-7 questions per category");
        prompt.Should().Contain("helpful tip");
    }

    [Fact]
    public void BuildAdditionalQuestionsPrompt_IncludesCandidateAndJobContext()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsPrompt(
            BuildAdditionalContext());

        prompt.Should().Contain("- Candidate Name: Ada Lovelace");
        prompt.Should().Contain("- Job Title: Backend Engineer");
        prompt.Should().Contain("- Company: Acme");
    }

    [Fact]
    public void BuildAdditionalQuestionsPrompt_IncludesExistingQuestionsUnderDoNotDuplicate()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsPrompt(
            BuildAdditionalContext());

        prompt.Should().Contain("EXISTING QUESTIONS (DO NOT DUPLICATE THESE):");
        prompt.Should().Contain("What is a closure?");
        prompt.Should().Contain("Tell me about a conflict.");
        prompt.Should().Contain("How do you scale a queue?");
        prompt.Should().Contain("Do not duplicate any of the previously generated questions");
    }

    [Fact]
    public void BuildAdditionalQuestionsSchema_RequestsAdditionalQuestionsSchema()
    {
        var schema = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsSchema();
        var json = System.Text.Json.JsonSerializer.Serialize(schema);

        json.Should().Contain("additionalQuestions");
    }

    [Fact]
    public void BuildAdditionalQuestionsPrompt_SpecifiesAdditionalQuestionVolume()
    {
        var prompt = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsPrompt(
            BuildAdditionalContext());

        prompt.Should().Contain("Generate 5-10 completely new questions");
    }
}
