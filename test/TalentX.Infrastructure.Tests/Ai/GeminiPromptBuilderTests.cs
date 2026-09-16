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
}
