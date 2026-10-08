namespace TalentX.Application.Features.Resumes;

public static class ResumeFileKindResolver
{
    public const string Pdf = "PDF";
    public const string Docx = "DOCX";

    public static string? Resolve(string? contentType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.Trim().Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return Pdf;

            if (contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase))
                return Docx;
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var extension = Path.GetExtension(fileName.Trim()).ToLowerInvariant();

            if (extension == ".pdf")
                return Pdf;

            if (extension is ".docx" or ".doc")
                return Docx;
        }

        return null;
    }
}
