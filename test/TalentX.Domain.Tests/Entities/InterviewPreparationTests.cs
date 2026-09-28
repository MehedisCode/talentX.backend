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
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: Guid.NewGuid(),
                TalentUserId: "talent-1",
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme Corp",
                InterviewType: "Technical",
                ScheduledAt: scheduledAt ?? DateTime.UtcNow.AddHours(2),
                TechnicalQuestions: "[\"What is REST?\"]",
                BehavioralQuestions: "[\"Tell me about yourself\"]",
                ExperienceBasedQuestions: "[\"Describe a tough project\"]",
                RoleSpecificQuestions: "[\"How do you scale a database?\"]",
                KeyTopicsToPrepare: "[\"HTTP\",\"SQL\"]",
                SuggestedRevisionAreas: "[\"Indexing\"]"));

        return preparation;
    }

    [Fact]
    public void Create_ValidArguments_SetsAllContentAndContextFields()
    {
        var invitationId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddDays(1);

        var preparation = InterviewPreparation.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: invitationId,
                TalentUserId: "talent-42",
                JobId: jobId,
                JobTitle: "Frontend Engineer",
                CompanyName: "Globex",
                InterviewType: "System Design",
                ScheduledAt: scheduledAt,
                TechnicalQuestions: "[\"What is CORS?\"]",
                BehavioralQuestions: "[\"Describe conflict with a coworker\"]",
                ExperienceBasedQuestions: "[\"Biggest career challenge?\"]",
                RoleSpecificQuestions: "[\"How do you optimize React renders?\"]",
                KeyTopicsToPrepare: "[\"Virtual DOM\",\"Memoization\"]",
                SuggestedRevisionAreas: "[\"Accessibility\"]"));

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
        var preparation = NewPreparation();

        preparation.Status.Should().Be(InterviewPreparationStatus.Generated);
        preparation.GenerationCount.Should().Be(1);
        preparation.AdditionalQuestions.Should().BeNull();
        preparation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        preparation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Regenerate_WithNewContent_ReplacesContentAndSetsRegeneratedStatus()
    {
        var preparation = NewPreparation();
        var oldTechnical = preparation.TechnicalQuestions;

        preparation.Regenerate(
            new RegenerateInterviewPreparationPayload(
                TechnicalQuestions: "[\"What is a thread pool?\"]",
                BehavioralQuestions: "[\"How do you handle pressure?\"]",
                ExperienceBasedQuestions: "[\"Failure you learned from?\"]",
                RoleSpecificQuestions: "[\"Explain CAP theorem\"]",
                KeyTopicsToPrepare: "[\"Concurrency\",\"Queues\"]",
                SuggestedRevisionAreas: "[\"Distributed locks\"]"));

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
        var preparation = NewPreparation();
        preparation.SetAdditionalQuestions("[\"What is DI?\"]");

        preparation.Regenerate(
            new RegenerateInterviewPreparationPayload(
                TechnicalQuestions: "[\"q1\"]",
                BehavioralQuestions: "[\"q2\"]",
                ExperienceBasedQuestions: "[\"q3\"]",
                RoleSpecificQuestions: "[\"q4\"]",
                KeyTopicsToPrepare: "[\"q5\"]",
                SuggestedRevisionAreas: "[\"q6\"]"));

        preparation.AdditionalQuestions.Should().BeNull();
    }

    [Fact]
    public void SetAdditionalQuestions_SetsValueAndUpdatedAt()
    {
        var preparation = NewPreparation();

        preparation.SetAdditionalQuestions("[\"What is the observer pattern?\"]");

        preparation.AdditionalQuestions.Should().Be("[\"What is the observer pattern?\"]");
        preparation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsAccessible_WhenScheduledAtIsNull_ReturnsTrue()
    {
        var preparation = NewPreparation(scheduledAt: null);

        var accessible = preparation.IsAccessible(TimeSpan.FromMinutes(30));

        accessible.Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_WithinThirtyMinutesAfterScheduledTime_ReturnsTrue()
    {
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-10));

        var accessible = preparation.IsAccessible(TimeSpan.FromMinutes(30));

        accessible.Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_AfterWindowExpires_ReturnsFalse()
    {
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-45));

        var accessible = preparation.IsAccessible(TimeSpan.FromMinutes(30));

        accessible.Should().BeFalse();
    }

    [Fact]
    public void IsAccessible_WithCustomWindow_UsesProvidedWindow()
    {
        var preparation = NewPreparation(scheduledAt: DateTime.UtcNow.AddMinutes(-20));

        var withinCustomWindow = preparation.IsAccessible(TimeSpan.FromMinutes(30));
        var outsideCustomWindow = preparation.IsAccessible(TimeSpan.FromMinutes(10));

        withinCustomWindow.Should().BeTrue();
        outsideCustomWindow.Should().BeFalse();
    }
}
