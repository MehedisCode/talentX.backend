using FluentAssertions;
using TalentX.Application.Common.Helpers;
using TalentX.Application.Features.TalentMatching.DTOs;
using Xunit;

namespace TalentX.Application.Tests.Common.Helpers;

public class MatchingInputHashTests
{
    private static JobProfileForMatching CreateJob(string description = "Build distributed systems.")
    {
        return new JobProfileForMatching
        {
            JobId = Guid.NewGuid(),
            Title = "Backend Engineer",
            CompanyName = "Acme",
            Description = description,
            RequiredSkills = new List<string> { "C#", "PostgreSQL" },
            ExperienceLevel = "SeniorLevel",
            Location = "Remote",
            WorkArrangement = "FullRemote",
            City = "Berlin",
            Country = "Germany"
        };
    }

    private static TalentProfileForMatching CreateCandidate()
    {
        return new TalentProfileForMatching
        {
            TalentUserId = "talent-1",
            Name = "Ada Lovelace",
            Tagline = "Analytical engine enthusiast",
            TechnicalSkills = new List<string> { "C#", ".NET" },
            SoftSkills = new List<string> { "Communication" },
            ToolsAndPlatforms = new List<string> { "Docker" },
            TotalYearsExperience = 8m,
            ExpectedSalary = 90000m,
            Location = "Berlin",
            SkillPropositions = new List<string> { "Distributed systems" },
            WorkExperiences = new List<WorkExperienceSummary>
            {
                new() { CompanyName = "Acme", JobTitle = "Senior Engineer", StartDate = "2020-01", EndDate = "2024-01", IsCurrent = false },
                new() { CompanyName = "Globex", JobTitle = "Engineer", StartDate = "2016-01", EndDate = "2019-12", IsCurrent = false }
            },
            Educations = new List<EducationSummary>
            {
                new() { Institution = "MIT", Degree = "BSc", StartDate = "2010", EndDate = "2014" },
                new() { Institution = "Stanford", Degree = "MSc", StartDate = "2014", EndDate = "2016" }
            }
        };
    }

    [Fact]
    public void Compute_ShuffledCollections_SameHash()
    {
        var job = CreateJob();
        var first = CreateCandidate();

        var second = CreateCandidate();
        second.TechnicalSkills.Reverse();
        second.SoftSkills.Reverse();
        second.ToolsAndPlatforms.Reverse();
        second.SkillPropositions.Reverse();
        second.WorkExperiences.Reverse();
        second.Educations.Reverse();

        var hashA = MatchingInputHash.Compute(job, first);
        var hashB = MatchingInputHash.Compute(job, second);

        hashB.Should().Be(hashA);
    }

    [Fact]
    public void Compute_StableAcrossCalls_ReturnsSameLowerCaseHex()
    {
        var job = CreateJob();
        var candidate = CreateCandidate();

        var hash = MatchingInputHash.Compute(job, candidate);

        hash.Should().Be(MatchingInputHash.Compute(job, candidate));
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Compute_ChangedJobDescription_DifferentHash()
    {
        var candidate = CreateCandidate();

        var hashA = MatchingInputHash.Compute(CreateJob("Build distributed systems."), candidate);
        var hashB = MatchingInputHash.Compute(CreateJob("Build mobile apps."), candidate);

        hashB.Should().NotBe(hashA);
    }

    [Fact]
    public void Compute_ChangedCandidateProfile_DifferentHash()
    {
        var job = CreateJob();
        var first = CreateCandidate();

        var second = CreateCandidate();
        second.TechnicalSkills.Add("Rust");

        var hashA = MatchingInputHash.Compute(job, first);
        var hashB = MatchingInputHash.Compute(job, second);

        hashB.Should().NotBe(hashA);
    }

    [Fact]
    public void Compute_DifferentScoringVersion_DifferentHash()
    {
        var job = CreateJob();
        var candidate = CreateCandidate();

        var hashA = MatchingInputHash.Compute(job, candidate);
        var hashB = MatchingInputHash.Compute(job, candidate, scoringVersion: "v-test");

        hashA.Should().NotBe(hashB);
        hashB.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
