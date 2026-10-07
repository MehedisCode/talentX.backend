using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TalentX.Infrastructure.TextExtraction;

public static class DocxDocumentParser
{
    private static readonly char[] TrailingTrimChars = ['.', ',', ';', ':', ')', ']', '>', '"', '\'', '|'];

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static (string PlainText, IReadOnlyList<string> Links) ExtractContent(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var doc = WordprocessingDocument.Open(fileStream, false);
        var mainPart = doc.MainDocumentPart;
        if (mainPart is null)
            return (string.Empty, Array.Empty<string>());

        var sb = new StringBuilder();
        var discoveredLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Extract hyperlinks from Relationships
        foreach (var rel in mainPart.HyperlinkRelationships)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rel.Uri is not null)
            {
                AddCleanLink(rel.Uri.ToString(), discoveredLinks);
            }
        }

        // 2. Extract paragraph text and hyperlinked elements
        var body = mainPart.Document?.Body;
        if (body is not null)
        {
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Extract text from individual runs and join with spaces to preserve word boundaries
                var runs = paragraph.Descendants<Text>()
                    .Select(t => t.Text)
                    .Where(t => !string.IsNullOrEmpty(t));
                var line = WhitespaceRegex.Replace(string.Join(" ", runs), " ").Trim();
                
                if (!string.IsNullOrEmpty(line))
                {
                    sb.AppendLine(line);
                }

                foreach (var hyperlink in paragraph.Descendants<Hyperlink>())
                {
                    var relId = hyperlink.Id?.Value;
                    if (!string.IsNullOrWhiteSpace(relId))
                    {
                        var rel = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == relId);
                        if (rel?.Uri is not null)
                        {
                            AddCleanLink(rel.Uri.ToString(), discoveredLinks);
                        }
                    }
                }
            }
        }

        return (sb.ToString(), discoveredLinks.ToList());
    }

    private static void AddCleanLink(string? rawUri, HashSet<string> set)
    {
        if (string.IsNullOrWhiteSpace(rawUri)) return;

        var clean = rawUri.Trim().TrimEnd(TrailingTrimChars);
        if (!string.IsNullOrWhiteSpace(clean))
        {
            set.Add(clean);
        }
    }
}
