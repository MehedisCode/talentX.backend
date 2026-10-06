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

    [Fact]
    public void BuildRequestBody_PromptIncludesNewExtractionFields()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
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
    public void BuildRequestBody_SchemaIncludesNewProperties()
    {
        var body = GeminiPromptBuilder.BuildRequestBody("resume text", Array.Empty<string>());
        var json = JsonSerializer.Serialize(body);

        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement
            .GetProperty("generationConfig")
            .GetProperty("response_schema")
            .GetProperty("properties");

        properties.TryGetProperty("tagline", out var taglineProperty).Should().BeTrue();
        taglineProperty.GetProperty("type").GetString().Should().Be("STRING");
        taglineProperty.GetProperty("description").GetString().Should().Be("Professional title or headline");

        properties.TryGetProperty("shortBio", out var shortBioProperty).Should().BeTrue();
        shortBioProperty.GetProperty("type").GetString().Should().Be("STRING");
        shortBioProperty.GetProperty("description").GetString().Should().Be("Executive summary or professional bio");

        properties.TryGetProperty("totalYearsExperience", out var experienceProperty).Should().BeTrue();
        experienceProperty.GetProperty("type").GetString().Should().Be("NUMBER");
        experienceProperty.GetProperty("description").GetString().Should().Be("Total years of professional experience");

        properties.TryGetProperty("expectedSalary", out var salaryProperty).Should().BeTrue();
        salaryProperty.GetProperty("type").GetString().Should().Be("NUMBER");
        salaryProperty.GetProperty("description").GetString().Should().Be("Expected salary or compensation if stated");

        properties.TryGetProperty("skillPropositions", out var propositionsProperty).Should().BeTrue();
        propositionsProperty.GetProperty("type").GetString().Should().Be("ARRAY");
        propositionsProperty.GetProperty("items").GetProperty("type").GetString().Should().Be("STRING");
        propositionsProperty.GetProperty("description").GetString().Should().Be("Key value propositions or skill highlights");
    }
}
