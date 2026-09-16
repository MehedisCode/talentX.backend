namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Deterministic post-filter for candidate portfolio links.
///
/// The AI is asked to classify the URLs found in a resume, but with no explicit
/// rule it tends to dump every non-LinkedIn URL (social profiles, job boards,
/// company or university sites, link shorteners, ...) into <c>portfolioUrls</c>.
/// This classifier keeps only URLs with a clear portfolio signal and drops
/// everything else — precision over recall, matching the ticket requirement
/// that only URLs explicitly identifiable as a portfolio are extracted.
/// </summary>
public static class PortfolioUrlClassifier
{
    // Exact hosts (or their subdomains) that are never a portfolio.
    private static readonly string[] DeniedHosts =
    [
        // Social & messaging
        "linkedin.com", "facebook.com", "fb.com", "instagram.com", "twitter.com", "x.com",
        "tiktok.com", "snapchat.com", "pinterest.com", "reddit.com", "youtube.com", "youtu.be",
        "whatsapp.com", "telegram.org", "t.me", "discord.com", "discord.gg", "threads.net",
        "vk.com", "weibo.com",
        // Job boards & freelance marketplaces
        "indeed.com", "glassdoor.com", "monster.com", "ziprecruiter.com", "dice.com",
        "careerbuilder.com", "upwork.com", "fiverr.com", "freelancer.com", "toptal.com",
        "naukri.com", "bdjobs.com", "wellfound.com", "angel.co",
        // Aggregators / shorteners / scheduling
        "bit.ly", "goo.gl", "tinyurl.com", "linktr.ee", "calendly.com",
    ];

    private static readonly string[] DeniedPathFragments = ["/jobs", "/job/", "/careers"];

    // Hosts that are portfolio-hosting platforms (or personal-site builders).
    private static readonly string[] AllowedHosts =
    [
        "behance.net", "dribbble.com", "artstation.com", "carbonmade.com",
        "github.io", "gitlab.io", "bitbucket.io", "pages.dev", "vercel.app", "netlify.app",
        "webflow.io", "firebaseapp.com", "readymag.com", "carrd.co", "about.me",
        "myportfolio.com", "squarespace.com", "wixsite.com", "wix.com", "weebly.com",
        "wordpress.com", "blogspot.com", "medium.com", "substack.com", "hashnode.dev",
        "dev.to", "notion.site", "ghost.io",
    ];

    // Keywords in the URL itself that signal a portfolio/personal site.
    private static readonly string[] UrlSignals =
    [
        "portfolio", "mywork", "my-work", "my_work", "myportfolio", "my-portfolio",
        "projects", "project", "personal", "website", "my-site", "mysite", "resume",
        "about-me", "aboutme", "blog",
    ];

    // Keywords in the resume line that contains the URL (anchor/label context).
    private static readonly string[] LabelSignals =
    [
        "portfolio", "personal website", "personal site", "personal blog", "website",
        "web site", "my work", "mywork", "projects", "project", "blog", "behance",
        "dribbble", "about me", "work samples", "sample work", "my site", "homepage",
        "home page",
    ];

    public static bool IsPortfolioUrl(string? url, string? context = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return false;
        if (!IsHttpScheme(uri)) return false;

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.ToLowerInvariant();
        var hostAndPath = host + path;

        // A developer profile page (github.com/<user>) counts as a portfolio;
        // deeper repo paths only qualify if they themselves carry a signal.
        if (IsDeveloperProfileHost(host) && SegmentCount(path) <= 1)
        {
            return true;
        }

        if (DeniedHosts.Any(denied => MatchesHost(host, denied)))
        {
            return false;
        }

        if (DeniedPathFragments.Any(fragment => path.Contains(fragment, StringComparison.Ordinal)))
        {
            return false;
        }

        if (AllowedHosts.Any(allowed => MatchesHost(host, allowed)))
        {
            return true;
        }

        if (UrlSignals.Any(signal => hostAndPath.Contains(signal, StringComparison.Ordinal)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context) &&
            LabelSignals.Any(signal => context.Contains(signal, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Filters a set of URLs down to those that look like genuine portfolio
    /// links, preserving order and removing duplicates/blank entries.
    /// </summary>
    public static List<string> Filter(IEnumerable<string>? urls, string? resumeText = null)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (urls is null) return result;

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;

            var trimmed = url.Trim();
            if (!seen.Add(trimmed)) continue;

            if (IsPortfolioUrl(trimmed, ExtractContext(resumeText, trimmed)))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    private static bool IsHttpScheme(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesHost(string host, string domain) =>
        host.Equals(domain, StringComparison.Ordinal) ||
        host.EndsWith($".{domain}", StringComparison.Ordinal);

    private static bool IsDeveloperProfileHost(string host) =>
        MatchesHost(host, "github.com") ||
        MatchesHost(host, "gitlab.com") ||
        MatchesHost(host, "bitbucket.org");

    private static int SegmentCount(string path) =>
        path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

    private static string? ExtractContext(string? resumeText, string url)
    {
        if (string.IsNullOrWhiteSpace(resumeText)) return null;

        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : url;

        var bareHost = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? host[4..]
            : host;

        foreach (var rawLine in resumeText.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            if (line.Contains(url, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(host, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(bareHost, StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }
}
