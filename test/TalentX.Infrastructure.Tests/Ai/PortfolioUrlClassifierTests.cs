using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class PortfolioUrlClassifierTests
{
    [Theory]
    [InlineData("https://www.linkedin.com/in/alice")]
    [InlineData("https://facebook.com/alice")]
    [InlineData("https://twitter.com/alice")]
    [InlineData("https://x.com/alice")]
    [InlineData("https://www.instagram.com/alice")]
    [InlineData("https://www.indeed.com/jobs?q=developer")]
    [InlineData("https://www.glassdoor.com/Profile/alice")]
    [InlineData("https://www.upwork.com/freelancers/alice")]
    [InlineData("https://bit.ly/abcd123")]
    [InlineData("https://acme.com")]
    [InlineData("https://www.stanford.edu")]
    [InlineData("mailto:alice@example.com")]
    [InlineData("tel:+15550001111")]
    [InlineData("https://github.com/alice/random-repo")]
    [InlineData("")]
    [InlineData("not a url")]
    public void IsPortfolioUrl_ReturnsFalse_ForNonPortfolioUrls(string url)
    {
        PortfolioUrlClassifier.IsPortfolioUrl(url).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.behance.net/alice")]
    [InlineData("https://dribbble.com/alice")]
    [InlineData("https://alice.github.io")]
    [InlineData("https://alice.github.io/portfolio")]
    [InlineData("https://alice.dev/portfolio")]
    [InlineData("https://example.com/my-work")]
    [InlineData("https://example.com/projects")]
    [InlineData("https://github.com/alice")]
    [InlineData("https://github.com/alice/portfolio")]
    [InlineData("https://janedoe.dev", "Portfolio: janedoe.dev")]
    [InlineData("https://janedoe.dev", "Personal Website | janedoe.dev")]
    public void IsPortfolioUrl_ReturnsTrue_ForPortfolioUrls(string url, string? context = null)
    {
        PortfolioUrlClassifier.IsPortfolioUrl(url, context).Should().BeTrue();
    }

    [Fact]
    public void Filter_KeepsOnlyPortfolioLinks_PreservingOrderAndDeduplicating()
    {
        var resumeText =
            """
            Alice Smith
            Portfolio: janedoe.dev
            LinkedIn: https://www.linkedin.com/in/alice
            """;

        var urls = new[]
        {
            "https://janedoe.dev",
            "https://www.linkedin.com/in/alice",
            "https://github.com/alice",
            "https://twitter.com/alice",
            "https://janedoe.dev"
        };

        var result = PortfolioUrlClassifier.Filter(urls, resumeText);

        result.Should().Equal("https://janedoe.dev", "https://github.com/alice");
    }

    [Fact]
    public void Filter_ReturnsEmpty_WhenNothingLooksLikeAPortfolio()
    {
        var urls = new[]
        {
            "https://www.linkedin.com/in/alice",
            "https://acme.com",
            "https://www.indeed.com/jobs"
        };

        PortfolioUrlClassifier.Filter(urls, "Alice Smith, Software Engineer").Should().BeEmpty();
    }

    [Fact]
    public void Filter_ReturnsEmpty_ForNullOrEmptyInput()
    {
        PortfolioUrlClassifier.Filter(null).Should().BeEmpty();
        PortfolioUrlClassifier.Filter([]).Should().BeEmpty();
    }

    [Theory]
    [InlineData("behance.net/aparna")]
    [InlineData("github.com/aparna")]
    [InlineData("alice.github.io/portfolio")]
    [InlineData("janedoe.dev", "Portfolio: janedoe.dev")]
    public void IsPortfolioUrl_AcceptsSchemeLessPortfolioUrls(string url, string? context = null)
    {
        PortfolioUrlClassifier.IsPortfolioUrl(url, context).Should().BeTrue();
    }

    [Theory]
    [InlineData("aparnakhatri.com")]
    [InlineData("www.aparnakhatri.dev")]
    [InlineData("aparna-khatri.com")]
    [InlineData("aparnakhatri.com/portfolio")]
    public void IsPortfolioUrl_AcceptsCandidatePersonalDomain_ByIdentityMatch(string url)
    {
        PortfolioUrlClassifier.IsPortfolioUrl(url, null, CandidateIdentity).Should().BeTrue();
    }

    [Fact]
    public void IsPortfolioUrl_RejectsUnrelatedDomain_EvenWithIdentity()
    {
        PortfolioUrlClassifier.IsPortfolioUrl("acme.com", null, CandidateIdentity).Should().BeFalse();
        PortfolioUrlClassifier.IsPortfolioUrl("stanford.edu", null, CandidateIdentity).Should().BeFalse();
    }

    [Fact]
    public void Filter_NormalizesSchemeLessUrls_AndUsesIdentity()
    {
        var urls = new[] { "aparnakhatri.com", "linkedin.com/in/aparnakhatri", "somecompany.com" };

        var result = PortfolioUrlClassifier.Filter(urls, "Aparna Khatri\naparnakhatri.com", CandidateIdentity);

        result.Should().Equal("https://aparnakhatri.com");
    }

    private static PortfolioIdentity CandidateIdentity =>
        new("Aparna Khatri", "aparna@example.com", "https://www.linkedin.com/in/aparnakhatri");
}
