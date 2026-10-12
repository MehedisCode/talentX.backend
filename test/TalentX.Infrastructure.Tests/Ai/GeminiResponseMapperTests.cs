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
        result.Tagline.Should().BeNull();
        result.ShortBio.Should().BeNull();
        result.TotalYearsExperience.Should().BeNull();
        result.ExpectedSalary.Should().BeNull();
        result.SkillPropositions.Should().BeEmpty();

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

    [Fact]
    public void MapToResult_MarksCurrentRole_WhenEndDateIsPresent()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "workExperience": [
            {
              "companyName": "Acme",
              "jobTitle": "Engineer",
              "startDate": "Jan 2020",
              "endDate": "Present"
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.WorkExperience[0].IsCurrent.Should().BeTrue();
        result.WorkExperience[0].EndDate.Should().Be("Present");
    }

    [Fact]
    public void MapToResult_MarksPastRole_AsNotCurrent()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "workExperience": [
            {
              "companyName": "Acme",
              "jobTitle": "Engineer",
              "startDate": "2017",
              "endDate": "2019"
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.WorkExperience[0].IsCurrent.Should().BeFalse();
        result.WorkExperience[0].EndDate.Should().Be("2019");
    }

    [Fact]
    public void MapToResult_KeepsDatelessWorkEntry()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "workExperience": [
            {
              "companyName": "Acme",
              "jobTitle": "Engineer",
              "startDate": ""
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.WorkExperience.Should().ContainSingle();
        result.WorkExperience[0].CompanyName.Should().Be("Acme");
        result.WorkExperience[0].IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void MapToResult_OrdersWorkExperience_NewestFirst_WithCurrentFirst()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "workExperience": [
            { "companyName": "Old Co", "jobTitle": "Developer", "startDate": "2016", "endDate": "2018" },
            { "companyName": "Present Co", "jobTitle": "Lead", "startDate": "Jan 2021", "endDate": "Present" },
            { "companyName": "Mid Co", "jobTitle": "Engineer", "startDate": "2019", "endDate": "2020" }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.WorkExperience.Select(w => w.CompanyName).Should().Equal("Present Co", "Mid Co", "Old Co");
    }

    [Fact]
    public void MapToResult_PutsUndatedWorkEntryLast()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "workExperience": [
            { "companyName": "No Dates", "jobTitle": "Intern", "startDate": "" },
            { "companyName": "Dated", "jobTitle": "Engineer", "startDate": "2020", "endDate": "2021" }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.WorkExperience.Select(w => w.CompanyName).Should().Equal("Dated", "No Dates");
    }

    [Fact]
    public void MapToResult_OrdersEducation_NewestGraduationFirst()
    {
        var json = """
        {
          "fullName": "Alice Smith",
          "education": [
            { "institution": "Old University", "degree": "B.Sc" , "startDate": "2012", "endDate": "2016" },
            { "institution": "New University", "degree": "M.Sc", "startDate": "2019", "endDate": "2021" }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Education.Select(e => e.Institution).Should().Equal("New University", "Old University");
    }

    [Fact]
    public void MapToResult_DetectsPersonalDomainPortfolio_ByIdentity()
    {
        var json = """
        {
          "fullName": "Aparna Khatri",
          "portfolioUrls": ["aparnakhatri.com"]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(
            json,
            "aparna@example.com",
            null,
            NullLogger.Instance,
            "Aparna Khatri\naparnakhatri.com");

        result.PortfolioUrls.Should().Equal("https://aparnakhatri.com");
    }

    [Fact]
    public void MapToResult_ShouldMapNewExtractionProperties_WhenPresent()
    {
        var jsonPayload = """
        {
          "fullName": "Alice Smith",
          "tagline": "Senior Full Stack Engineer",
          "shortBio": "Over 8 years building scalable cloud services.",
          "totalYearsExperience": 8.5,
          "expectedSalary": 120000,
          "skillPropositions": [
            "Distributed Systems Architecture",
            "High Throughput APIs"
          ]
        }
        """;

        var extractionResult = GeminiResponseMapper.MapToResult(
            jsonPayload,
            null,
            null,
            NullLogger.Instance);

        extractionResult.Tagline.Should().Be("Senior Full Stack Engineer");
        extractionResult.ShortBio.Should().Be("Over 8 years building scalable cloud services.");
        extractionResult.TotalYearsExperience.Should().Be(8.5m);
        extractionResult.ExpectedSalary.Should().Be(120000m);
        extractionResult.SkillPropositions.Should().Equal(
            "Distributed Systems Architecture",
            "High Throughput APIs");
    }

    [Fact]
    public void MapToResult_ShouldDefaultNewExtractionPropertiesToNullOrEmpty_WhenMissingFromJson()
    {
        var jsonPayload = """
        {
          "fullName": "Alice Smith"
        }
        """;

        var extractionResult = GeminiResponseMapper.MapToResult(
            jsonPayload,
            null,
            null,
            NullLogger.Instance);

        extractionResult.Tagline.Should().BeNull();
        extractionResult.ShortBio.Should().BeNull();
        extractionResult.TotalYearsExperience.Should().BeNull();
        extractionResult.ExpectedSalary.Should().BeNull();
        extractionResult.SkillPropositions.Should().BeEmpty();
    }

    [Fact]
    public void MapToResult_ShouldTrimStringsAndFilterWhitespaceSkillPropositions()
    {
        var jsonPayload = """
        {
          "fullName": "Alice Smith",
          "tagline": "   Staff Engineer   ",
          "shortBio": "   Experienced technical lead specializing in cloud systems.   ",
          "skillPropositions": [
            "   Cloud Native   ",
            "   ",
            "",
            "   System Design   "
          ]
        }
        """;

        var extractionResult = GeminiResponseMapper.MapToResult(
            jsonPayload,
            null,
            null,
            NullLogger.Instance);

        extractionResult.Tagline.Should().Be("Staff Engineer");
        extractionResult.ShortBio.Should().Be("Experienced technical lead specializing in cloud systems.");
        extractionResult.SkillPropositions.Should().Equal("Cloud Native", "System Design");
    }

    [Fact]
    public void MapToResult_ShouldNormalizeNegativeNumbersAndWhitespaceStringsToNull()
    {
        var jsonPayload = """
        {
          "fullName": "Alice Smith",
          "tagline": "   ",
          "shortBio": "",
          "totalYearsExperience": -3.5,
          "expectedSalary": -75000
        }
        """;

        var extractionResult = GeminiResponseMapper.MapToResult(
            jsonPayload,
            null,
            null,
            NullLogger.Instance);

        extractionResult.Tagline.Should().BeNull();
        extractionResult.ShortBio.Should().BeNull();
        extractionResult.TotalYearsExperience.Should().BeNull();
        extractionResult.ExpectedSalary.Should().BeNull();
    }

    [Fact]
    public void MapToResult_ShouldAcceptZeroForExperienceAndSalary()
    {
        var jsonPayload = """
        {
          "fullName": "Alice Smith",
          "totalYearsExperience": 0,
          "expectedSalary": 0
        }
        """;

        var extractionResult = GeminiResponseMapper.MapToResult(
            jsonPayload,
            null,
            null,
            NullLogger.Instance);

        extractionResult.TotalYearsExperience.Should().Be(0m);
        extractionResult.ExpectedSalary.Should().Be(0m);
    }

    [Fact]
    public void MapToResult_ShouldMapProjects_WhenProjectsPresent()
    {
        var json = """
        {
          "fullName": "Bob Builder",
          "projects": [
            {
              "projectName": "  Talent Platform  ",
              "role": "  Lead Architect  ",
              "projectType": "professional",
              "startDate": "2023-01",
              "endDate": "Present",
              "isOngoing": true,
              "description": "  Built a scalable hiring marketplace.  ",
              "technologiesUsed": [" C# ", " .NET 10 ", ".NET 10", "PostgreSQL"],
              "projectUrl": "  https://github.com/example/talent-platform  "
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Projects.Should().HaveCount(1);
        var project = result.Projects[0];
        project.ProjectName.Should().Be("Talent Platform");
        project.Role.Should().Be("Lead Architect");
        project.ProjectType.Should().Be("Professional");
        project.StartDate.Should().Be("2023-01");
        project.EndDate.Should().Be("Present");
        project.IsOngoing.Should().BeTrue();
        project.Description.Should().Be("Built a scalable hiring marketplace.");
        project.TechnologiesUsed.Should().ContainInOrder("C#", ".NET 10", "PostgreSQL");
        project.ProjectUrl.Should().Be("https://github.com/example/talent-platform");
    }

    [Fact]
    public void MapToResult_ShouldSortProjects_NewestAndOngoingFirst()
    {
        var json = """
        {
          "fullName": "Bob Builder",
          "projects": [
            {
              "projectName": "Old Academic Project",
              "startDate": "2019-01",
              "endDate": "2019-12",
              "isOngoing": false
            },
            {
              "projectName": "Current Ongoing Project",
              "startDate": "2024-01",
              "endDate": "Present",
              "isOngoing": true
            },
            {
              "projectName": "Recent Finished Project",
              "startDate": "2022-01",
              "endDate": "2023-06",
              "isOngoing": false
            },
            {
              "projectName": "Undated Project"
            }
          ]
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Projects.Should().HaveCount(4);
        result.Projects[0].ProjectName.Should().Be("Current Ongoing Project");
        result.Projects[1].ProjectName.Should().Be("Recent Finished Project");
        result.Projects[2].ProjectName.Should().Be("Old Academic Project");
        result.Projects[3].ProjectName.Should().Be("Undated Project");
    }

    [Fact]
    public void MapToResult_ShouldHandleNullOrEmptyProjectsGracefully()
    {
        var json = """
        {
          "fullName": "Bob Builder",
          "projects": null
        }
        """;

        var result = GeminiResponseMapper.MapToResult(json, null, null, NullLogger.Instance);

        result.Projects.Should().NotBeNull();
        result.Projects.Should().BeEmpty();
    }
}
