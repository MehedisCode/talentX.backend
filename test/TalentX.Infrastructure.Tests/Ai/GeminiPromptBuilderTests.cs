using System.Text.Json;
using FluentAssertions;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiPromptBuilderTests
{
    // Education dates are optional in the schema (startDate is not required),
    // but the prompt strongly instructs Gemini to extract them. The normalizer
    // handles single-year degrees (moves lone year to endDate). This test
    // verifies the schema does not require startDate, allowing Gemini to
    // follow the prompt instruction to leave it empty for single-year degrees.
    [Fact]
    public void BuildRequestBody_EducationDoesNotRequireStartDate()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var required = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("responseJsonSchema")
            .GetProperty("properties")
            .GetProperty("education")
            .GetProperty("items")
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        required.Should().Contain("institution");
        required.Should().Contain("degree");
        required.Should().NotContain("startDate");
    }

    [Fact]
    public void BuildRequestBody_PromptCoversEducationDateRanges()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("put the first year in 'startDate' and the second year in 'endDate'");
    }

    [Fact]
    public void BuildRequestBody_OrdersWorkExperienceBeforeSkillArrays()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var schema = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("responseJsonSchema");

        var propertyOrder = schema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToList();

        propertyOrder.IndexOf("workExperience").Should().BeLessThan(propertyOrder.IndexOf("technicalSkills"));

        var requiredOrder = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString() ?? string.Empty)
            .ToList();

        requiredOrder.IndexOf("workExperience").Should().BeLessThan(requiredOrder.IndexOf("technicalSkills"));
    }

    // The work item's 'startDate' must NOT be required: structured output forces
    // values for required fields, so a required startDate made the model invent
    // dates (or a "Present" end date) for dateless roles (TX26-433). 'companyName'
    // and 'jobTitle' must stay required so roles themselves aren't dropped.
    [Fact]
    public void BuildRequestBody_WorkItemRequiresNameAndTitle_ButNotStartDate()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var workItem = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("responseJsonSchema")
            .GetProperty("properties")
            .GetProperty("workExperience")
            .GetProperty("items");

        var required = workItem
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        required.Should().Contain("companyName");
        required.Should().Contain("jobTitle");
        required.Should().NotContain("startDate");

        workItem.GetProperty("properties").TryGetProperty("isCurrent", out _).Should().BeTrue();
    }

    [Fact]
    public void BuildRequestBody_PromptGuardsCurrentRoleClassification()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("Only treat a role as CURRENT");
        prompt.Should().Contain("If the resume gives NO dates for a role, leave both date fields empty and set 'isCurrent' to false");
    }

    [Fact]
    public void BuildRequestBody_SetsMaxOutputTokens()
    {
        var options = new GeminiPromptBuildOptions(MaxOutputTokens: 1234);
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>(), options);
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var maxOutputTokens = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("max_output_tokens")
            .GetInt32();

        maxOutputTokens.Should().Be(1234);
    }

    [Fact]
    public void BuildRequestBody_IncludesDeterministicContactInformation_WhenProvided()
    {
        var options = new GeminiPromptBuildOptions(
            DeterministicEmail: "test@example.com",
            DeterministicPhone: "+1234567890");

        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>(), options);
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("EXTRACTED CONTACT INFORMATION:");
        prompt.Should().Contain("Email: test@example.com");
        prompt.Should().Contain("Phone: +1234567890");
    }

    [Fact]
    public void BuildRequestBody_PromptTellsModelToExtractEveryRole()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("Extract EVERY work experience entry");
    }

    [Fact]
    public void BuildRequestBody_PromptCoversMonthYearPresentRange()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("(December 2024 - Present)");
    }

    [Theory]
    [InlineData(ResumeExtractionPurpose.JobApplication)]
    [InlineData(null)]
    public void BuildRequestBody_WhenJobApplicationOrDefault_PromptDoesNotContainProfileFields(ResumeExtractionPurpose? purpose)
    {
        var body = purpose.HasValue
            ? GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>(), purpose.Value)
            : GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().NotBeNull();
        prompt.Should().NotContain("tagline:");
        prompt.Should().NotContain("shortBio:");
        prompt.Should().NotContain("totalYearsExperience:");
        prompt.Should().NotContain("expectedSalary:");
        prompt.Should().NotContain("skillPropositions:");
    }

    [Theory]
    [InlineData(ResumeExtractionPurpose.JobApplication)]
    [InlineData(null)]
    public void BuildRequestBody_WhenJobApplicationOrDefault_SchemaDoesNotContainProfileProperties(ResumeExtractionPurpose? purpose)
    {
        var body = purpose.HasValue
            ? GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>(), purpose.Value)
            : GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("responseJsonSchema")
            .GetProperty("properties");

        properties.TryGetProperty("tagline", out _).Should().BeFalse();
        properties.TryGetProperty("shortBio", out _).Should().BeFalse();
        properties.TryGetProperty("totalYearsExperience", out _).Should().BeFalse();
        properties.TryGetProperty("expectedSalary", out _).Should().BeFalse();
        properties.TryGetProperty("skillPropositions", out _).Should().BeFalse();

        properties.TryGetProperty("fullName", out _).Should().BeTrue();
        properties.TryGetProperty("email", out _).Should().BeTrue();
        properties.TryGetProperty("phone", out _).Should().BeTrue();
        properties.TryGetProperty("location", out _).Should().BeTrue();
        properties.TryGetProperty("linkedInUrl", out _).Should().BeTrue();
        properties.TryGetProperty("portfolioUrls", out _).Should().BeTrue();
        properties.TryGetProperty("workExperience", out _).Should().BeTrue();
        properties.TryGetProperty("education", out _).Should().BeTrue();
        properties.TryGetProperty("technicalSkills", out _).Should().BeTrue();
        properties.TryGetProperty("softSkills", out _).Should().BeTrue();
        properties.TryGetProperty("toolsAndPlatforms", out _).Should().BeTrue();
    }

    [Fact]
    public void BuildRequestBody_PromptIncludesEdgeCaseInstructions()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().Contain("multiple LinkedIn URLs");
        prompt.Should().Contain("redacted or masked");
        prompt.Should().Contain("invalid or impossible dates");
    }

    [Fact]
    public void BuildRequestBody_WhenCandidateProfile_PromptContainsProfileFields()
    {
        var body = GeminiPromptBuilder.BuildRequestBody(
            "resume text",
            Array.Empty<string>(),
            ResumeExtractionPurpose.CandidateProfile);
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var prompt = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        prompt.Should().NotBeNull();
        prompt.Should().Contain("tagline: Headline / professional title");
        prompt.Should().Contain("shortBio: Executive summary / about me / professional summary");
        prompt.Should().Contain("totalYearsExperience: Total years of professional experience as a number");
        prompt.Should().Contain("expectedSalary: Desired/expected salary or compensation");
        prompt.Should().Contain("skillPropositions: Key strengths, value propositions");
    }

    [Fact]
    public void BuildRequestBody_WhenCandidateProfile_SchemaContainsProfileProperties()
    {
        var body = GeminiPromptBuilder.BuildRequestBody(
            "resume text",
            Array.Empty<string>(),
            ResumeExtractionPurpose.CandidateProfile);
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("responseJsonSchema")
            .GetProperty("properties");

        properties.TryGetProperty("tagline", out var taglineProperty).Should().BeTrue();
        taglineProperty.GetProperty("type").GetString().Should().Be("string");
        taglineProperty.GetProperty("description").GetString().Should().Be("Professional title or headline");

        properties.TryGetProperty("shortBio", out var shortBioProperty).Should().BeTrue();
        shortBioProperty.GetProperty("type").GetString().Should().Be("string");
        shortBioProperty.GetProperty("description").GetString().Should().Be("Executive summary or professional bio");

        properties.TryGetProperty("totalYearsExperience", out var experienceProperty).Should().BeTrue();
        experienceProperty.GetProperty("type").GetString().Should().Be("number");
        experienceProperty.GetProperty("description").GetString().Should().Be("Total years of professional experience");

        properties.TryGetProperty("expectedSalary", out var salaryProperty).Should().BeTrue();
        salaryProperty.GetProperty("type").GetString().Should().Be("number");
        salaryProperty.GetProperty("description").GetString().Should().Be("Expected salary or compensation if stated");

        properties.TryGetProperty("skillPropositions", out var propositionsProperty).Should().BeTrue();
        propositionsProperty.GetProperty("type").GetString().Should().Be("array");
        propositionsProperty.GetProperty("items").GetProperty("type").GetString().Should().Be("string");
        propositionsProperty.GetProperty("description").GetString().Should().Be("Key value propositions or skill highlights");
    }
}
