using FluentAssertions;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Infrastructure.Ai.Prompts;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai.Prompts;

public class V2Tests
{
    private static TalentMatchingRequest SampleRequest()
    {
        return new TalentMatchingRequest
        {
            Job = new JobProfileForMatching
            {
                JobId = Guid.NewGuid(),
                Title = "Backend Engineer",
                CompanyName = "Acme",
                Description = "Build distributed systems.",
                RequiredSkills = new List<string> { "C#" }
            },
            Candidates = new List<TalentProfileForMatching>
            {
                new() { TalentUserId = "talent-1", Name = "Ada Lovelace" }
            }
        };
    }

    [Fact]
    public void Version_ReturnsV2()
    {
        new V2().Version.Should().Be("v2");
    }

    [Fact]
    public void BuildPrompt_ContainsAbsoluteScoringInstructions()
    {
        var prompt = new V2().BuildPrompt(SampleRequest());

        prompt.Should().Contain("ABSOLUTE SCORING RULES (CRITICAL):");
        prompt.Should().Contain("Score EVERY candidate INDEPENDENTLY against the job posting only.");
        prompt.Should().Contain("NEVER compare, rank, curve, or normalize scores against the other candidates");
        prompt.Should().Contain("A perfect match between a candidate and the job posting must score 100");
    }

    [Fact]
    public void BuildPrompt_ContainsCandidatesSectionAndResponseSchema()
    {
        var prompt = new V2().BuildPrompt(SampleRequest());

        prompt.Should().Contain("CANDIDATES:");
        prompt.Should().Contain("\"matches\"");
        prompt.Should().Contain("\"talentUserId\"");
        prompt.Should().Contain("\"score\"");
        prompt.Should().Contain("\"explanation\"");
        prompt.Should().Contain("Return exactly one \"matches\" entry per candidate listed above");
    }

    [Fact]
    public void BuildPrompt_IncludesJobAndCandidateJson()
    {
        var prompt = new V2().BuildPrompt(SampleRequest());

        prompt.Should().Contain("Backend Engineer");
        prompt.Should().Contain("talent-1");
        prompt.Should().Contain("Ada Lovelace");
    }

    [Fact]
    public void Factory_KnownVersion_ReturnsMatchingPrompt()
    {
        var factory = new TalentMatchingPromptFactory(new ITalentMatchingPrompt[] { new V1(), new V2() });

        factory.GetPrompt("v2").Should().BeOfType<V2>();
        factory.GetPrompt("v1").Should().BeOfType<V1>();
        factory.GetPrompt("V2").Should().BeOfType<V2>();
    }

    [Fact]
    public void Factory_NullOrUnknownVersion_FallsBackToV1()
    {
        var factory = new TalentMatchingPromptFactory(new ITalentMatchingPrompt[] { new V1(), new V2() });

        factory.GetPrompt().Should().BeOfType<V1>();
        factory.GetPrompt(null).Should().BeOfType<V1>();
        factory.GetPrompt("v99").Should().BeOfType<V1>();
        factory.GetPrompt("   ").Should().BeOfType<V1>();
    }

    [Fact]
    public void V1_Version_IsV1()
    {
        new V1().Version.Should().Be("v1");
    }
}
