using FluentAssertions;
using TalentX.Application.Features.CompanyReviews;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews;

public class CompanyReviewSummaryCalculatorTests
{
    private static RatingAggregate None => new(0, 0);

    [Fact]
    public void Empty_HasZeroCountsAndNoAverages()
    {
        var summary = CompanyReviewSummaryCalculator.Empty;

        summary.TotalCount.Should().Be(0);
        summary.PositivePercent.Should().Be(0);
        summary.RecruitmentProcess.Average.Should().BeNull();
        summary.Professionalism.RatingCount.Should().Be(0);
    }

    [Fact]
    public void Create_DerivesNegativeCountFromTotalAndPositive()
    {
        var summary = CompanyReviewSummaryCalculator.Create(
            new CompanyReviewAggregates(10, 7, None, None, None, None));

        summary.NegativeCount.Should().Be(3);
        summary.PositivePercent.Should().Be(70m);
    }

    [Theory]
    [InlineData(1, 1, 100)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 3, 33.3)]
    [InlineData(2, 3, 66.7)]
    public void Create_RoundsPositivePercentToOneDecimal(int positive, int total, double expected)
    {
        var summary = CompanyReviewSummaryCalculator.Create(
            new CompanyReviewAggregates(total, positive, None, None, None, None));

        summary.PositivePercent.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData(17, 4, 4.3)]   // 4.25 rounds half up
    [InlineData(14, 3, 4.7)]   // 4.666…
    [InlineData(5, 1, 5.0)]
    [InlineData(7, 4, 1.8)]    // 1.75
    public void Create_RoundsAverageToOneDecimal(int sum, int count, double expected)
    {
        var summary = CompanyReviewSummaryCalculator.Create(
            new CompanyReviewAggregates(count, count, new RatingAggregate(count, sum), None, None, None));

        summary.RecruitmentProcess.Average.Should().Be((decimal)expected);
        summary.RecruitmentProcess.RatingCount.Should().Be(count);
    }

    [Fact]
    public void Create_AreaWithNoRatings_HasNullAverage()
    {
        var summary = CompanyReviewSummaryCalculator.Create(
            new CompanyReviewAggregates(2, 1, None, new RatingAggregate(2, 9), None, None));

        summary.RecruitmentProcess.Average.Should().BeNull();
        summary.Communication.Average.Should().Be(4.5m);
    }
}
