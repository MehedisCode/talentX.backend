using System.Text.Json;
using FluentAssertions;
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
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>(), null, null, 1234);
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var maxOutputTokens = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("max_output_tokens")
            .GetInt32();

        maxOutputTokens.Should().Be(1234);
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
}
