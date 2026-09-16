using System.Text.Json;
using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiPromptBuilderTests
{
    // Requiring 'startDate' is what makes Gemini reliably emit education dates
    // at all; when it was optional the model skipped the dates entirely. This
    // guards that regression (see TX26-320 #3).
    [Fact]
    public void BuildRequestBody_RequiresEducationStartDate()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var required = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("response_schema")
            .GetProperty("properties")
            .GetProperty("education")
            .GetProperty("items")
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        required.Should().Contain("institution");
        required.Should().Contain("degree");
        required.Should().Contain("startDate");
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
            .GetProperty("response_schema");

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

    [Fact]
    public void BuildRequestBody_RequiresWorkItemStartDateAndExposesIsCurrent()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var workItem = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("response_schema")
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
        required.Should().Contain("startDate");

        workItem.GetProperty("properties").TryGetProperty("isCurrent", out _).Should().BeTrue();
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
}
