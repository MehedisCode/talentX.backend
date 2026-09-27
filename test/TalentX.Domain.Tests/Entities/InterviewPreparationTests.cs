using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class InterviewPreparationTests
{
    private static InterviewPreparation NewPreparation(DateTime? scheduledAt = null)
    {
        var preparation = InterviewPreparation.Create(
            interviewInvitationId: Guid.NewGuid(),
            talentUserId: "talent-1",
            jobId: Guid.NewGuid(),
            jobTitle: "Backend Engineer",
            companyName: "Acme Corp",
            interviewType: "Technical",
            scheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
            technicalQuestions: "[\"What is REST?\"]",
            behavioralQuestions: "[\"Tell me about yourself\"]",
            experienceBasedQuestions: "[\"Describe a tough project\"]",
            roleSpecificQuestions: "[\"How do you scale a database?\"]",
            keyTopicsToPrepare: "[\"HTTP\",\"SQL\"]",
            suggestedRevisionAreas: "[\"Indexing\"]");

        return preparation;
    }

    [Fact]
    public void Create_ValidArguments_SetsAllContentAndContextFields()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddDays(1);

        // Act
        var preparation = InterviewPreparation.Create(
            invitationId,
            "talent-42",
            jobId,
            "Frontend Engineer",
            "Globex",
            "System Design",
            scheduledAt,
            "[\"What is CORS?\"]",
            "[\"Describe conflict with a coworker\"]",
            "[\"Biggest career challenge?\"]",
            "[\"How do you optimize React renders?\"]",
            "[\"Virtual DOM\",\"Memoization\"]",
            "[\"Accessibility\"]");

        // Assert
        preparation.InterviewInvitationId.Should().Be(invitationId);
        preparation.TalentUserId.Should().Be("talent-42");
        preparation.JobId.Should().Be(jobId);
        preparation.JobTitle.Should().Be("Frontend Engineer");
        preparation.CompanyName.Should().Be("Globex");
        preparation.InterviewType.Should().Be("System Design");
        preparation.ScheduledAt.Should().Be(scheduledAt);
        preparation.TechnicalQuestions.Should().Be("[\"What is CORS?\"]");
        preparation.BehavioralQuestions.Should().Be("[\"Describe conflict with a coworker\"]");
        preparation.ExperienceBasedQuestions.Should().Be("[\"Biggest career challenge?\"]");
        preparation.RoleSpecificQuestions.Should().Be("[\"How do you optimize React renders?\"]");
        preparation.KeyTopicsToPrepare.Should().Be("[\"Virtual DOM\",\"Memoization\"]");
        preparation.SuggestedRevisionAreas.Should().Be("[\"Accessibility\"]");
    }

    [Fact]
    public void Create_ValidArguments_SetsGeneratedStatusAndFirstGeneration()
    {
        // Act
        var preparation = NewPreparation();

        // Assert
        preparation.Status.Should().Be(InterviewPreparationStatus.Generated);
        preparation.GenerationCount.Should().Be(1);
        preparation.AdditionalQuestions.Should().BeNull();
        preparation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        preparation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Regenerate_WithNewContent_ReplacesContentAndSetsRegeneratedStatus()
    {
        // Arrange
        var preparation = NewPreparation();
        var oldTechnical = preparation.TechnicalQuestions;

        // Act
        preparation.Regenerate(
            "[\"What is a thread pool?\"]",
            "[\"How do you handle pressure?\"]",
            "[\"Failure you learned from?\"]",
            "[\"Explain CAP theorem\"]",
            "[\"Concurrency\",\"Queues\"]",
            "[\"Distributed locks\"]");

        // Assert
        preparation.Status.Should().Be(InterviewPreparationStatus.Regenerated);
        preparation.GenerationCount.Should().Be(2);
        preparation.TechnicalQuestions.Should().Be("[\"What is a thread pool?\"]")
            .And.NotBe(oldTechnical);
        preparation.BehavioralQuestions.Should().Be("[\"How do you handle pressure?\"]");
        preparation.ExperienceBasedQuestions.Should().Be("[\"Failure you learned from?\"]");
        preparation.RoleSpecificQuestions.Should().Be("[\"Explain CAP theorem\"]");
        preparation.KeyTopicsToPrepare.Should().Be("[\"Concurrency\",\"Queues\"]");
        preparation.SuggestedRevisionAreas.Should().Be("[\"Distributed locks\"]");
        preparation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Regenerate_WhenAdditionalQuestionsExist_ClearsThem()
    {
        // Arrange
        var preparation = NewPreparation();
        preparation.SetAdditionalQuestions("[\"What is DI?\"]");

        // Act
        preparation.Regenerate(
            "[\"q1\"]", "[\"q2\"]", "[\"q3\"]", "[\"q4\"]", "[\"q5\"]", "[\"q6\"]");

        // Assert
        preparation.AdditionalQuestions.Should().BeNull();
    }

    [Fact]
    public void SetAdditionalQuestions_SetsValueAndUpdatedAt()
    {
        // Arrange
        var preparation = NewPreparation();

        // Act
        preparation.SetAdditionalQuestions("[\"What is the observer pattern?\"]");

        // Assert
        preparation.AdditionalQuestions.Should().Be("[\"What is the observer pattern?\"]");
        preparation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsAccessible_WhenScheduledAtIsNull_ReturnsTrue()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: null);

        // Act
        var accessible = preparation.IsAccessible();

        // Assert
        accessible.Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_WithinThirtyMinutesAfterScheduledTime_ReturnsTrue()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-10));

        // Act
        var accessible = preparation.IsAccessible();

        // Assert
        accessible.Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_AfterDefaultWindowExpires_ReturnsFalse()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-45));

        // Act
        var accessible = preparation.IsAccessible();

        // Assert
        accessible.Should().BeFalse();
    }

    [Fact]
    public void IsAccessible_WithCustomWindow_UsesProvidedWindow()
    {
        // Arrange
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-20));

        // Act
        var withinCustomWindow = preparation.IsAccessible(TimeSpan.FromMinutes(30));
        var outsideCustomWindow = preparation.IsAccessible(TimeSpan.FromMinutes(10));

        // Assert
        withinCustomWindow.Should().BeTrue();
        outsideCustomWindow.Should().BeFalse();
    }
}
