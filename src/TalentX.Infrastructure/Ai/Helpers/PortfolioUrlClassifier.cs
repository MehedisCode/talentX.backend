using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Identity of the candidate, used to recognize a personal domain
/// (e.g. "aparnakhatri.com" for "Aparna Khatri") as their portfolio.
/// </summary>
public sealed record PortfolioIdentity(string? FullName, string? Email, string? LinkedInUrl);

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

    // Public suffixes made of two labels (so the registrable label is the third from the end).
    private static readonly string[] TwoPartSuffixes =
    [
        "co.uk", "org.uk", "ac.uk", "gov.uk", "com.au", "net.au", "org.au", "co.nz",
        "co.jp", "com.br", "com.bd", "com.pk", "co.in", "co.za", "com.mx", "com.ar",
        "com.sg", "com.my", "com.ph", "co.id", "com.tr",
    ];

    private static readonly Regex ExplicitSchemeRegex =
        new(@"^[a-z][a-z0-9+.\-]*:", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LinkedInVanityRegex =
        new(@"linkedin\.com/in/([^/?#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsPortfolioUrl(string? url, string? context = null, PortfolioIdentity? identity = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        var normalized = NormalizeUrl(url);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)) return false;
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

        return MatchesIdentity(host, identity);
    }

    /// <summary>
    /// Filters a set of URLs down to those that look like genuine portfolio
    /// links, preserving order and removing duplicates/blank entries. Returned
    /// URLs are normalized to include an http(s) scheme.
    /// </summary>
    public static List<string> Filter(
        IEnumerable<string>? urls,
        string? resumeText = null,
        PortfolioIdentity? identity = null)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (urls is null) return result;

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;

            var normalized = NormalizeUrl(url);
            if (!seen.Add(normalized)) continue;

            if (IsPortfolioUrl(normalized, ExtractContext(resumeText, normalized), identity))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();

        if (ExplicitSchemeRegex.IsMatch(trimmed))
        {
            return trimmed;
        }

        return $"https://{trimmed}";
    }

    private static bool MatchesIdentity(string host, PortfolioIdentity? identity)
    {
        if (identity is null) return false;

        var label = RegistrableLabel(host);
        if (label.Length < 4) return false;

        foreach (var token in BuildIdentityTokens(identity))
        {
            if (token.Length < 4) continue;

            if (label.Equals(token, StringComparison.Ordinal) ||
                label.Contains(token, StringComparison.Ordinal) ||
                token.Contains(label, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> BuildIdentityTokens(PortfolioIdentity identity)
    {
        yield return NormalizeToken(identity.FullName);
        yield return NormalizeToken(EmailLocalPart(identity.Email));
        yield return NormalizeToken(LinkedInVanity(identity.LinkedInUrl));
    }

    private static string NormalizeToken(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? EmailLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : null;
    }

    private static string? LinkedInVanity(string? linkedInUrl)
    {
        if (string.IsNullOrWhiteSpace(linkedInUrl)) return null;

        var match = LinkedInVanityRegex.Match(linkedInUrl);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string RegistrableLabel(string host)
    {
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 2) return NormalizeToken(host);

        var suffixStart = labels.Length - 1;
        if (labels.Length >= 3)
        {
            var twoPart = $"{labels[^2]}.{labels[^1]}";
            if (TwoPartSuffixes.Contains(twoPart, StringComparer.OrdinalIgnoreCase))
            {
                suffixStart = labels.Length - 2;
            }
        }

        return NormalizeToken(labels[suffixStart - 1]);
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
