using FluentAssertions;
using TalentX.Application.Features.Resumes;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class ResumeFileKindResolverTests
{
    [Theory]
    [InlineData("application/pdf", "anything.pdf", "PDF")]
    [InlineData("application/pdf", "no-extension-name", "PDF")]
    [InlineData("application/octet-stream", "Naib Uddin CV Resume.docx", "DOCX")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "cv.docx", "DOCX")]
    [InlineData("application/msword", "old-cv.doc", "DOCX")]
    [InlineData(null, "resume.docx", "DOCX")]
    [InlineData(null, "weird.NAME.PDF", "PDF")]
    [InlineData("application/octet-stream", "no-extension-name", null)]
    [InlineData("text/plain", "notes.txt", null)]
    [InlineData(null, null, null)]
    [InlineData("", "", null)]
    [InlineData("application/pdf", null, "PDF")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", null, "DOCX")]
    public void Resolve_IdentifiesPdfAndDocxFromMimeOrExtension(
        string? contentType,
        string? fileName,
        string? expected)
    {
        var result = ResumeFileKindResolver.Resolve(contentType, fileName);

        result.Should().Be(expected);
    }
}
