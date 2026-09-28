using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class InterviewPreparationRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static InterviewPreparation NewPreparation(
        string talentUserId = "talent-1",
        Guid? interviewInvitationId = null)
    {
        return InterviewPreparation.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: interviewInvitationId ?? Guid.NewGuid(),
                TalentUserId: talentUserId,
                JobId: Guid.NewGuid(),
                JobTitle: "Backend Engineer",
                CompanyName: "Acme Corp",
                InterviewType: "Technical",
                ScheduledAt: DateTime.UtcNow.AddDays(1),
                TechnicalQuestions: "[\"What is REST?\"]",
                BehavioralQuestions: "[\"Tell me about yourself\"]",
                ExperienceBasedQuestions: "[\"Describe a tough project\"]",
                RoleSpecificQuestions: "[\"How do you scale a database?\"]",
                KeyTopicsToPrepare: "[\"HTTP\",\"SQL\"]",
                SuggestedRevisionAreas: "[\"Indexing\"]"));
    }

    [Fact]
    public async Task GetByIdAsync_WhenPreparationExists_ReturnsIt()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();
        context.InterviewPreparations.Add(preparation);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(preparation.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(preparation.Id);
        result.TalentUserId.Should().Be("talent-1");
    }

    [Fact]
    public async Task GetByIdAsync_WhenPreparationDoesNotExist_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByInvitationIdAsync_WhenInvitationMatches_ReturnsPreparation()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var invitationId = Guid.NewGuid();
        var preparation = NewPreparation(interviewInvitationId: invitationId);
        var other = NewPreparation(interviewInvitationId: Guid.NewGuid());
        context.InterviewPreparations.AddRange(preparation, other);
        await context.SaveChangesAsync();

        var result = await repository.GetByInvitationIdAsync(invitationId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(preparation.Id);
    }

    [Fact]
    public async Task GetByInvitationIdAsync_WhenInvitationDoesNotMatch_ReturnsNull()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        context.InterviewPreparations.Add(NewPreparation());
        await context.SaveChangesAsync();

        var result = await repository.GetByInvitationIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTalentUserIdAsync_ReturnsOnlyThatTalentsPreparationsOrderedByCreatedAtDescending()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var oldest = NewPreparation();
        var middle = NewPreparation();
        var newest = NewPreparation();
        var otherTalent = NewPreparation(talentUserId: "talent-2");
        context.InterviewPreparations.AddRange(oldest, middle, newest, otherTalent);

        context.Entry(oldest).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-3);
        context.Entry(middle).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-2);
        context.Entry(newest).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-1);
        await context.SaveChangesAsync();

        var result = await repository.GetByTalentUserIdAsync("talent-1");

        result.Should().HaveCount(3);
        result.Select(x => x.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task AddAsync_SavesPreparationToContext()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();

        await repository.AddAsync(preparation);
        await context.SaveChangesAsync();

        var stored = await context.InterviewPreparations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == preparation.Id);
        stored.Should().NotBeNull();
        stored!.JobTitle.Should().Be("Backend Engineer");
        stored.Status.Should().Be(TalentX.Domain.Enums.InterviewPreparationStatus.Generated);
    }

    [Fact]
    public async Task Update_PersistsRegeneratedContent()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();
        context.InterviewPreparations.Add(preparation);
        await context.SaveChangesAsync();

        preparation.Regenerate(
            new RegenerateInterviewPreparationPayload(
                TechnicalQuestions: "[\"What is a thread pool?\"]",
                BehavioralQuestions: "[\"How do you handle pressure?\"]",
                ExperienceBasedQuestions: "[\"Failure you learned from?\"]",
                RoleSpecificQuestions: "[\"Explain CAP theorem\"]",
                KeyTopicsToPrepare: "[\"Concurrency\"]",
                SuggestedRevisionAreas: "[\"Distributed locks\"]"));
        repository.Update(preparation);
        await context.SaveChangesAsync();

        var stored = await context.InterviewPreparations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == preparation.Id);
        stored.Should().NotBeNull();
        stored!.TechnicalQuestions.Should().Be("[\"What is a thread pool?\"]");
        stored.GenerationCount.Should().Be(2);
        stored.AdditionalQuestions.Should().BeNull();
    }

    [Fact]
    public async Task CountTodayByTalentAsync_SumsGenerationCountOfTodayRowsOnly()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var first = NewPreparation();
        var regenerated = NewPreparation();
        var yesterday = NewPreparation();
        var otherTalent = NewPreparation(talentUserId: "talent-2");
        regenerated.Regenerate(
            new RegenerateInterviewPreparationPayload(
                "[\"n1\"]", "[\"n2\"]", "[\"n3\"]", "[\"n4\"]", "[\"n5\"]", "[\"n6\"]"));
        context.InterviewPreparations.AddRange(first, regenerated, yesterday, otherTalent);

        context.Entry(yesterday).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var count = await repository.CountTodayByTalentAsync("talent-1");

        count.Should().Be(3);
    }

    [Fact]
    public async Task CountTodayByTalentAsync_WhenNoRowsToday_ReturnsZero()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);

        var count = await repository.CountTodayByTalentAsync("talent-unknown");

        count.Should().Be(0);
    }
}
