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
        Guid? interviewInvitationId = null,
        Guid? jobId = null)
    {
        return InterviewPreparation.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: interviewInvitationId ?? Guid.NewGuid(),
                TalentUserId: talentUserId,
                JobId: jobId ?? Guid.NewGuid(),
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
    public async Task CountInitialGenerationsTodayByTalentAsync_CountsOnlyInitialEventsForTalent()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();
        context.InterviewPreparations.Add(preparation);
        await context.SaveChangesAsync();

        var initial = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));
        var regenerate = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Regenerate));
        var additional = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.AdditionalQuestions));
        var yesterday = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));
        var otherTalent = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-2",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));
        context.InterviewPreparationGenerations
            .AddRange(initial, regenerate, additional, yesterday, otherTalent);
        context.Entry(yesterday).Property(x => x.CreatedAt).CurrentValue =
            DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var count = await repository.CountInitialGenerationsTodayByTalentAsync("talent-1");

        count.Should().Be(1);
    }

    [Fact]
    public async Task CountAllGenerationsTodayByTalentAsync_CountsEveryKindForTalent()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();
        context.InterviewPreparations.Add(preparation);
        await context.SaveChangesAsync();

        var initial = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));
        var regenerate = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Regenerate));
        var yesterday = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));
        var otherTalent = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-2",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Regenerate));
        context.InterviewPreparationGenerations
            .AddRange(initial, regenerate, yesterday, otherTalent);
        context.Entry(yesterday).Property(x => x.CreatedAt).CurrentValue =
            DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var count = await repository.CountAllGenerationsTodayByTalentAsync("talent-1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task CountAllGenerationsTodayByTalentAsync_WhenNoEventsToday_ReturnsZero()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);

        var count = await repository.CountAllGenerationsTodayByTalentAsync("talent-unknown");

        count.Should().Be(0);
    }

    [Fact]
    public async Task AddGenerationAsync_SavesGenerationEventToContext()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation();
        context.InterviewPreparations.Add(preparation);
        await context.SaveChangesAsync();

        var generation = InterviewPreparationGeneration.Create(
            new CreateGenerationPayload(preparation.Id, "talent-1",
                TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial));

        await repository.AddGenerationAsync(generation);
        await context.SaveChangesAsync();

        var stored = await context.InterviewPreparationGenerations
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == generation.Id);
        stored.Should().NotBeNull();
        stored!.InterviewPreparationId.Should().Be(preparation.Id);
        stored.Kind.Should().Be(
            TalentX.Domain.Enums.InterviewPreparationGenerationKind.Initial);
    }

    [Fact]
    public async Task DeleteByTalentUserIdAsync_DeletesOnlyThatTalentsPreparations()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var preparation = NewPreparation(talentUserId: "talent-1");
        var otherTalent = NewPreparation(talentUserId: "talent-2");
        context.InterviewPreparations.AddRange(preparation, otherTalent);
        await context.SaveChangesAsync();

        await repository.DeleteByTalentUserIdAsync("talent-1");
        await context.SaveChangesAsync();

        context.InterviewPreparations.Should().ContainSingle(x => x.TalentUserId == "talent-2");
    }

    [Fact]
    public async Task DeleteByTalentUserIdAsync_WhenTalentHasNoPreparations_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        context.InterviewPreparations.Add(NewPreparation(talentUserId: "talent-1"));
        await context.SaveChangesAsync();

        await repository.DeleteByTalentUserIdAsync("talent-unknown");
        await context.SaveChangesAsync();

        context.InterviewPreparations.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteByJobIdsAsync_DeletesPreparationsMatchingJobIds()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var preparation1 = NewPreparation(jobId: jobId1);
        var preparation2 = NewPreparation(jobId: jobId2);
        var otherJob = NewPreparation();
        context.InterviewPreparations.AddRange(preparation1, preparation2, otherJob);
        await context.SaveChangesAsync();

        await repository.DeleteByJobIdsAsync(new[] { jobId1, jobId2 });
        await context.SaveChangesAsync();

        context.InterviewPreparations.Should().ContainSingle(x => x.Id == otherJob.Id);
    }

    [Fact]
    public async Task DeleteByJobIdsAsync_WhenIdsEmpty_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new InterviewPreparationRepository(context);
        context.InterviewPreparations.Add(NewPreparation());
        await context.SaveChangesAsync();

        await repository.DeleteByJobIdsAsync(Array.Empty<Guid>());
        await context.SaveChangesAsync();

        context.InterviewPreparations.Should().ContainSingle();
    }
}
