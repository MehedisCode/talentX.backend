using FluentAssertions;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Domain.Entities;
using Xunit;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Mappers;

public class InterviewPreparationMapperTests
{
    private static readonly TimeSpan AccessWindow = TimeSpan.FromMinutes(30);

    private static InterviewPreparationEntity NewPreparation(
        string technicalQuestions = """[{"question":"What is a closure?","category":"Technical","tip":"Scope"}]""",
        string behavioralQuestions = """[{"question":"Tell me about a conflict","category":"Behavioral","tip":"STAR"}]""",
        string experienceBasedQuestions = "[]",
        string roleSpecificQuestions = "[]",
        string keyTopicsToPrepare = """["Idempotency"]""",
        string suggestedRevisionAreas = """["SQL tuning"]""",
        DateTime? scheduledAt = null)
    {
        var preparation = InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: "talent-1",
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme",
                InterviewType: "OnCall",
                ScheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: technicalQuestions,
                BehavioralQuestions: behavioralQuestions,
                ExperienceBasedQuestions: experienceBasedQuestions,
                RoleSpecificQuestions: roleSpecificQuestions,
                KeyTopicsToPrepare: keyTopicsToPrepare,
                SuggestedRevisionAreas: suggestedRevisionAreas));

        return preparation;
    }

    [Fact]
    public void ToDto_WithValidJson_DeserializesQuestionAndTopicLists()
    {
        // Arrange
        var preparation = NewPreparation();

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.TechnicalQuestions.Should().ContainSingle()
            .Which.Should().Be(new PrepQuestion("What is a closure?", "Technical", "Scope"));
        dto.BehavioralQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("Tell me about a conflict");
        dto.KeyTopicsToPrepare.Should().Equal("Idempotency");
        dto.SuggestedRevisionAreas.Should().Equal("SQL tuning");
        dto.ExperienceBasedQuestions.Should().BeEmpty();
        dto.RoleSpecificQuestions.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithMalformedJson_ReturnsEmptyListsWithoutThrowing()
    {
        // Arrange
        var preparation = NewPreparation(
            technicalQuestions: "{not valid json",
            behavioralQuestions: """{"key":"object instead of list"}""",
            keyTopicsToPrepare: "42",
            suggestedRevisionAreas: "");

        // Act — a throw here fails the test; corrupt fields must degrade to empty lists
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.TechnicalQuestions.Should().BeEmpty();
        dto.BehavioralQuestions.Should().BeEmpty();
        dto.KeyTopicsToPrepare.Should().BeEmpty();
        dto.SuggestedRevisionAreas.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithBlankQuestionFields_ReturnsEmptyLists()
    {
        // Arrange
        var preparation = NewPreparation(
            technicalQuestions: "",
            behavioralQuestions: "   ",
            keyTopicsToPrepare: "",
            suggestedRevisionAreas: "");

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.TechnicalQuestions.Should().BeEmpty();
        dto.BehavioralQuestions.Should().BeEmpty();
        dto.KeyTopicsToPrepare.Should().BeEmpty();
        dto.SuggestedRevisionAreas.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WhenAdditionalQuestionsAbsent_ReturnsEmptyList()
    {
        // Arrange
        var preparation = NewPreparation();

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert — uniform fallback policy: absent data maps to [], never null
        dto.AdditionalQuestions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ToDto_WhenAdditionalQuestionsPresent_DeserializesThem()
    {
        // Arrange
        var preparation = NewPreparation();
        preparation.SetAdditionalQuestions(
            """[{"question":"What is DI?","category":"Technical","tip":"Containers"}]""");

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.AdditionalQuestions.Should().ContainSingle()
            .Which.Question.Should().Be("What is DI?");
    }

    [Fact]
    public void ToDto_MapsScalarFieldsAndStatusString()
    {
        // Arrange
        var preparation = NewPreparation();

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.Id.Should().Be(preparation.Id);
        dto.InterviewInvitationId.Should().Be(preparation.InterviewInvitationId);
        dto.JobId.Should().Be(preparation.JobId);
        dto.JobTitle.Should().Be("Backend Engineer");
        dto.CompanyName.Should().Be("Acme");
        dto.InterviewType.Should().Be("OnCall");
        dto.ScheduledAt.Should().Be(preparation.ScheduledAt);
        dto.Status.Should().Be("Generated");
        dto.GenerationCount.Should().Be(1);
        dto.GeneratedAt.Should().Be(preparation.CreatedAt);
        dto.Disclaimer.Should().NotBeEmpty();
    }

    [Fact]
    public void ToDto_ComputesIsAccessibleTrue_ForUpcomingInterview()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddHours(3));

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ComputesIsAccessibleFalse_WhenInterviewWindowExpired()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddHours(-2));

        // Act
        var dto = InterviewPreparationMapper.ToDto(preparation, AccessWindow);

        // Assert
        dto.IsAccessible.Should().BeFalse();
    }
}
