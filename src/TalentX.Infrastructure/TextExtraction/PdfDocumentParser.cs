using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Tokens;

namespace TalentX.Infrastructure.TextExtraction;

public static class PdfDocumentParser
{
    private static readonly char[] TrailingTrimChars = ['.', ',', ';', ':', ')', ']', '>', '"', '\'', '|'];

    public static (string PlainText, IReadOnlyList<string> Links) ExtractContent(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var pdf = PdfDocument.Open(fileStream);
        var sb = new StringBuilder();
        var discoveredLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExtractPageText(page, sb);
            ExtractPageLinks(page, discoveredLinks);
        }

        return (sb.ToString(), discoveredLinks.ToList());
    }

    private static void ExtractPageText(Page page, StringBuilder sb)
    {
        var words = page.GetWords()?.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left).ToList();
        if (words is not null && words.Count > 0)
        {
            var lines = new List<List<Word>>();
            const double lineThreshold = 4.0;

            foreach (var word in words)
            {
                var matchingLine = lines.FirstOrDefault(l =>
                    Math.Abs(l[0].BoundingBox.Bottom - word.BoundingBox.Bottom) < lineThreshold);

                if (matchingLine != null)
                {
                    matchingLine.Add(word);
                }
                else
                {
                    lines.Add(new List<Word> { word });
                }
            }

            foreach (var line in lines)
            {
                var lineText = string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                sb.AppendLine(lineText);
            }
        }
        else if (!string.IsNullOrWhiteSpace(page.Text))
        {
            sb.AppendLine(page.Text);
        }
    }

    private static void ExtractPageLinks(Page page, HashSet<string> discoveredLinks)
    {
        try
        {
            foreach (var annot in page.ExperimentalAccess.GetAnnotations())
            {
                var dict = annot.AnnotationDictionary;
                if (dict is null) continue;

                if (dict.TryGet(NameToken.A, out var actionToken) && actionToken is DictionaryToken actionDict)
                {
                    if (actionDict.TryGet(NameToken.Uri, out var uriToken))
                    {
                        AddCleanLink(GetTokenStringValue(uriToken), discoveredLinks);
                    }
                }

                if (dict.TryGet(NameToken.Uri, out var directUriToken))
                {
                    AddCleanLink(GetTokenStringValue(directUriToken), discoveredLinks);
                }
            }
        }
        catch
        {
            // Ignore annotation dictionary parsing issues gracefully
        }

        try
        {
            foreach (var link in page.GetHyperlinks())
            {
                AddCleanLink(link.Uri, discoveredLinks);
            }
        }
        catch
        {
            // Ignore hyperlink parsing issues gracefully
        }
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

    private static string? GetTokenStringValue(IToken token)
    {
        return token switch
        {
            StringToken str => str.Data,
            HexToken hex => hex.Data,
            NameToken name => name.Data,
            _ => token?.ToString()
        };
    }
}
