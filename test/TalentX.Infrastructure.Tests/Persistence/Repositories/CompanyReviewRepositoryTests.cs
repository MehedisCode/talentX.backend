using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class CompanyReviewRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CompanyReview MakeReview(
        Guid? companyId = null,
        Guid? applicationId = null,
        string talentUserId = "talent-1",
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        int? recruitment = null,
        int? communication = null,
        int? interview = null,
        int? professionalism = null)
    {
        return CompanyReview.Create(
            companyId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            applicationId ?? Guid.NewGuid(),
            talentUserId,
            "Backend Engineer",
            sentiment,
            "Comment",
            recruitment,
            communication,
            interview,
            professionalism);
    }

    private static CompanyReview MakePublished(
        Guid companyId,
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        int? recruitment = null,
        int? communication = null,
        int? interview = null,
        int? professionalism = null)
    {
        var review = MakeReview(companyId, null, "talent-1", sentiment, recruitment, communication, interview, professionalism);
        review.Approve("admin-1");
        return review;
    }

    private static async Task SeedAsync(ApplicationDbContext context, params CompanyReview[] reviews)
    {
        context.CompanyReviews.AddRange(reviews);
        await context.SaveChangesAsync();
    }

    // ---- CRUD ----

    [Fact]
    public async Task AddAsync_PersistsReview()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var review = MakeReview(recruitment: 4);

        await repository.AddAsync(review);
        await context.SaveChangesAsync();

        var saved = await context.CompanyReviews.SingleAsync();
        saved.Id.Should().Be(review.Id);
        saved.Status.Should().Be(CompanyReviewStatus.Pending);
        saved.RecruitmentProcessRating.Should().Be(4);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsReview_OrNullWhenMissing()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var review = MakeReview();
        await SeedAsync(context, review);

        (await repository.GetByIdAsync(review.Id))!.Id.Should().Be(review.Id);
        (await repository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetByJobApplicationIdAsync_ReturnsReviewForThatApplicationOnly()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var review = MakeReview();
        await SeedAsync(context, review, MakeReview());

        (await repository.GetByJobApplicationIdAsync(review.JobApplicationId))!.Id.Should().Be(review.Id);
        (await repository.GetByJobApplicationIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetByJobApplicationIdsAsync_ReturnsOnlyRequestedApplications()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var first = MakeReview();
        var second = MakeReview();
        var other = MakeReview();
        await SeedAsync(context, first, second, other);

        var result = await repository.GetByJobApplicationIdsAsync(new[] { first.JobApplicationId, second.JobApplicationId });

        result.Select(r => r.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
    }

    [Fact]
    public async Task GetByJobApplicationIdsAsync_EmptyInput_ReturnsEmpty()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        await SeedAsync(context, MakeReview());

        var result = await repository.GetByJobApplicationIdsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByTalentUserIdAsync_RemovesOnlyThatTalentsReviews()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var mine = MakeReview(talentUserId: "talent-1");
        var alsoMine = MakeReview(talentUserId: "talent-1");
        var theirs = MakeReview(talentUserId: "talent-2");
        await SeedAsync(context, mine, alsoMine, theirs);

        await repository.DeleteByTalentUserIdAsync("talent-1");
        await context.SaveChangesAsync();

        (await context.CompanyReviews.Select(r => r.Id).ToListAsync()).Should().BeEquivalentTo(new[] { theirs.Id });
    }

    [Fact]
    public async Task DeleteByCompanyIdAsync_RemovesOnlyThatCompanysReviews()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();
        var inCompany = MakeReview(companyId);
        var elsewhere = MakeReview();
        await SeedAsync(context, inCompany, elsewhere);

        await repository.DeleteByCompanyIdAsync(companyId);
        await context.SaveChangesAsync();

        (await context.CompanyReviews.Select(r => r.Id).ToListAsync()).Should().BeEquivalentTo(new[] { elsewhere.Id });
    }

    [Fact]
    public async Task Delete_WhenNothingMatches_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        await SeedAsync(context, MakeReview());

        await repository.DeleteByTalentUserIdAsync("nobody");
        await repository.DeleteByCompanyIdAsync(Guid.NewGuid());
        await context.SaveChangesAsync();

        (await context.CompanyReviews.CountAsync()).Should().Be(1);
    }

    // ---- GetPublishedSummaryAsync ----

    [Fact]
    public async Task GetPublishedSummaryAsync_NoReviews_ReturnsZerosAndNulls()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);

        var summary = await repository.GetPublishedSummaryAsync(Guid.NewGuid());

        summary.TotalCount.Should().Be(0);
        summary.PositiveCount.Should().Be(0);
        summary.NegativeCount.Should().Be(0);
        summary.PositivePercent.Should().Be(0);
        summary.RecruitmentProcess.Should().Be(new RatingAreaSummaryDto(null, 0));
        summary.Communication.Average.Should().BeNull();
        summary.InterviewExperience.RatingCount.Should().Be(0);
        summary.Professionalism.Average.Should().BeNull();
    }

    [Fact]
    public async Task GetPublishedSummaryAsync_IgnoresEveryNonPublishedStatus()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();

        var pending = MakeReview(companyId, recruitment: 1);

        var rejected = MakeReview(companyId, recruitment: 1);
        rejected.Reject(ReviewModerationReason.Spam, "spam", "admin-1");

        var hidden = MakePublished(companyId, recruitment: 1);
        hidden.Hide(ReviewModerationReason.Other, "hidden", "admin-1");

        var withdrawn = MakeReview(companyId, recruitment: 1);
        withdrawn.Withdraw();

        var published = MakePublished(companyId, recruitment: 5);

        await SeedAsync(context, pending, rejected, hidden, withdrawn, published);

        var summary = await repository.GetPublishedSummaryAsync(companyId);

        summary.TotalCount.Should().Be(1);
        summary.RecruitmentProcess.Average.Should().Be(5m);
        summary.RecruitmentProcess.RatingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPublishedSummaryAsync_IgnoresOtherCompaniesReviews()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();
        await SeedAsync(
            context,
            MakePublished(companyId, ReviewSentiment.Positive),
            MakePublished(Guid.NewGuid(), ReviewSentiment.Negative));

        var summary = await repository.GetPublishedSummaryAsync(companyId);

        summary.TotalCount.Should().Be(1);
        summary.PositiveCount.Should().Be(1);
        summary.NegativeCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPublishedSummaryAsync_AveragesIgnoreNullRatingsPerArea()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();
        await SeedAsync(
            context,
            MakePublished(companyId, recruitment: 5, communication: 2),
            MakePublished(companyId, recruitment: 3),
            MakePublished(companyId, interview: 4));

        var summary = await repository.GetPublishedSummaryAsync(companyId);

        summary.TotalCount.Should().Be(3);
        summary.RecruitmentProcess.Average.Should().Be(4m);
        summary.RecruitmentProcess.RatingCount.Should().Be(2);
        summary.Communication.Average.Should().Be(2m);
        summary.Communication.RatingCount.Should().Be(1);
        summary.InterviewExperience.Average.Should().Be(4m);
        summary.InterviewExperience.RatingCount.Should().Be(1);
        summary.Professionalism.Average.Should().BeNull();
        summary.Professionalism.RatingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPublishedSummaryAsync_CountsSentimentAndPercent()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();
        await SeedAsync(
            context,
            MakePublished(companyId, ReviewSentiment.Positive),
            MakePublished(companyId, ReviewSentiment.Negative),
            MakePublished(companyId, ReviewSentiment.Negative));

        var summary = await repository.GetPublishedSummaryAsync(companyId);

        summary.TotalCount.Should().Be(3);
        summary.PositiveCount.Should().Be(1);
        summary.NegativeCount.Should().Be(2);
        summary.PositivePercent.Should().Be(33.3m);
    }

    [Fact]
    public async Task GetPublishedSummaryAsync_RoundsToOneDecimal_HalvesRoundUp()
    {
        await using var context = NewContext();
        var repository = new CompanyReviewRepository(context);
        var companyId = Guid.NewGuid();
        // recruitment 4,4,4,5 = 4.25 -> 4.3 (banker's rounding would give 4.2)
        // communication 5,5,4 = 4.666… -> 4.7
        await SeedAsync(
            context,
            MakePublished(companyId, recruitment: 4, communication: 5),
            MakePublished(companyId, recruitment: 4, communication: 5),
            MakePublished(companyId, recruitment: 4, communication: 4),
            MakePublished(companyId, recruitment: 5));

        var summary = await repository.GetPublishedSummaryAsync(companyId);

        summary.RecruitmentProcess.Average.Should().Be(4.3m);
        summary.Communication.Average.Should().Be(4.7m);
    }
}
