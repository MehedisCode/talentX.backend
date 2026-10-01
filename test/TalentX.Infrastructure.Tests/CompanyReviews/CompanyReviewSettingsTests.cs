using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TalentX.Infrastructure.CompanyReviews;
using Xunit;

namespace TalentX.Infrastructure.Tests.CompanyReviews;

public class CompanyReviewSettingsTests
{
    private static IConfiguration Config(string? value)
    {
        var values = new Dictionary<string, string?>();
        if (value is not null)
        {
            values["CompanyReview:AssessmentReviewDelayDays"] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static int Resolve(string? configValue) =>
        CompanyReviewSettings.FromConfiguration(Config(configValue)).AssessmentReviewDelayDays;

    [Fact]
    public void FromConfiguration_NothingConfigured_UsesDefaultOfOne()
    {
        Resolve(configValue: null).Should().Be(1);
    }

    [Fact]
    public void FromConfiguration_ReadsConfigSection()
    {
        Resolve(configValue: "3").Should().Be(3);
    }

    [Fact]
    public void FromConfiguration_ZeroIsAllowed()
    {
        Resolve(configValue: "0").Should().Be(0);
    }

    [Fact]
    public void FromConfiguration_NegativeValue_ClampsToZero()
    {
        Resolve(configValue: "-4").Should().Be(0);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("")]
    public void FromConfiguration_UnparseableValue_FallsBackToDefault(string raw)
    {
        Resolve(configValue: raw).Should().Be(1);
    }
}
