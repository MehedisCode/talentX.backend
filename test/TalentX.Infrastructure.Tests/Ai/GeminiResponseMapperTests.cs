using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class GeminiResponseMapperTests
{
    [Fact]
    public void MapToResult_ShouldDeserializeJsonAndMapToResult()
    {
        // Arrange
        var json = """
        {
          "fullName": "Alice Smith",
          "email": "alice@example.com",
          "phone": "+1-555-987-6543",
          "location": "New York, NY",
          "linkedInUrl": "https://linkedin.com/in/alicesmith",
          "portfolioUrls": ["https://github.com/alicesmith"],
          "technicalSkills": ["C#", ".NET 10", "PostgreSQL"],
          "softSkills": ["Leadership", "Communication"],
          "toolsAndPlatforms": ["Docker", "AWS"],
          "workExperience": [
            {
              "companyName": "Tech Corp",
              "jobTitle": "Senior Software Engineer",
              "startDate": "2021-01",
              "endDate": "Present"
            }
          ],
          "education": [
            {
              "institution": "MIT",
              "degree": "B.S. Computer Science",
              "startDate": "2016-09",
              "endDate": "2020-05"
            }
          ]
        }
        """;

        // Act
        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Alice Smith");
        result.Email.Should().Be("alice@example.com");
        result.Phone.Should().Be("+1-555-987-6543");
        result.Location.Should().Be("New York, NY");
        result.LinkedInUrl.Should().Be("https://linkedin.com/in/alicesmith");
        result.PortfolioUrls.Should().ContainSingle("https://github.com/alicesmith");
        result.TechnicalSkills.Should().Contain(new[] { "C#", ".NET 10", "PostgreSQL" });
        result.SoftSkills.Should().Contain(new[] { "Leadership", "Communication" });
        result.ToolsAndPlatforms.Should().Contain(new[] { "Docker", "AWS" });

        result.WorkExperience.Should().HaveCount(1);
        result.WorkExperience[0].CompanyName.Should().Be("Tech Corp");
        result.WorkExperience[0].JobTitle.Should().Be("Senior Software Engineer");
        result.WorkExperience[0].StartDate.Should().Be("2021-01");
        result.WorkExperience[0].EndDate.Should().Be("Present");

        result.Education.Should().HaveCount(1);
        result.Education[0].Institution.Should().Be("MIT");
        result.Education[0].Degree.Should().Be("B.S. Computer Science");
    }

    [Fact]
    public void MapToResult_ShouldPreferDeterministicEmailAndPhone_WhenProvided()
    {
        // Arrange
        var json = """
        {
          "fullName": "Alice Smith",
          "email": null,
          "phone": null
        }
        """;
        var deterministicEmail = "alice.deterministic@example.com";
        var deterministicPhone = "+1-555-000-1111";

        // Act
        var result = GeminiResponseMapper.MapToResult(json, deterministicEmail, deterministicPhone, NullLogger.Instance);

        // Assert
        result.Email.Should().Be("alice.deterministic@example.com");
        result.Phone.Should().Be("+1-555-000-1111");
    }

    [Fact]
    public void MapToResult_MovesLoneEducationYear_ToEndDate()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "education": [
            {
              "institution": "MIT",
              "degree": "B.S. Computer Science",
              "startDate": "2019",
              "endDate": null
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Education.Should().ContainSingle();
        result.Education[0].StartDate.Should().BeNull();
        result.Education[0].EndDate.Should().Be("2019");
    }

    [Fact]
    public void MapToResult_KeepsEducationRange_WhenTwoYearsProvided()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "education": [
            {
              "institution": "MIT",
              "degree": "B.S. Computer Science",
              "startDate": "2017",
              "endDate": "2021"
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Education[0].StartDate.Should().Be("2017");
        result.Education[0].EndDate.Should().Be("2021");
    }

    [Fact]
    public void MapToResult_SplitsEducationRangeReturnedInOneField()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "education": [
            {
              "institution": "MIT",
              "degree": "B.S. Computer Science",
              "startDate": "2016 - 2020",
              "endDate": ""
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Education[0].StartDate.Should().Be("2016");
        result.Education[0].EndDate.Should().Be("2020");
    }
}
