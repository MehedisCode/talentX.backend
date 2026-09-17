using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class TextUrlExtractorTests
{
    [Theory]
    [InlineData("Portfolio: aparnakhatri.com", "https://aparnakhatri.com")]
    [InlineData("Website - www.aparnakhatri.dev/portfolio", "https://www.aparnakhatri.dev/portfolio")]
    [InlineData("behance.net/aparna", "https://behance.net/aparna")]
    [InlineData("github.com/aparna", "https://github.com/aparna")]
    [InlineData("carrd.co", "https://carrd.co")]
    public void CombineAndDeduplicate_CapturesBareDomains_WithHttpsScheme(string text, string expected)
    {
        var links = TextUrlExtractor.CombineAndDeduplicate(text, Array.Empty<string>());

        links.Should().Contain(expected);
    }

    [Fact]
    public void CombineAndDeduplicate_DoesNotTreatEmailDomainAsUrl()
    {
        var links = TextUrlExtractor.CombineAndDeduplicate(
            "Email: alice@example.com",
            Array.Empty<string>());

        links.Should().NotContain(link => link.Contains("example.com", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Experience with Node.js and C#")]
    [InlineData("Version v1.2.3 released")]
    [InlineData("Archive file.tar.gz")]
    public void CombineAndDeduplicate_IgnoresNonUrls(string text)
    {
        TextUrlExtractor.CombineAndDeduplicate(text, Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public void CombineAndDeduplicate_KeepsSchemedUrlsWithoutDuplicating()
    {
        var links = TextUrlExtractor.CombineAndDeduplicate(
            "See https://example.com/portfolio",
            Array.Empty<string>());

        links.Should().Equal("https://example.com/portfolio");
    }

    [Fact]
    public void CombineAndDeduplicate_KeepsDocumentLinks()
    {
        var links = TextUrlExtractor.CombineAndDeduplicate(
            "no links here",
            ["https://aparnakhatri.com"]);

        links.Should().ContainSingle().Which.Should().Be("https://aparnakhatri.com");
    }
}
