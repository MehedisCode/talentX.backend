using FluentAssertions;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Talents;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents;

public class TalentMediaStorageKeyGuardTests
{
    [Fact]
    public void EnsureOwnedByUser_WithMatchingCategoryAndUserId_DoesNotThrow()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes/user-1/x.pdf", "user-1", "resumes");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("resumes/user-1/../../../etc/passwd", "user-1", "resumes")]
    [InlineData("resumes/../user-1/x.pdf", "user-1", "resumes")]
    [InlineData("resumes/./user-1/x.pdf", "user-1", "resumes")]
    public void EnsureOwnedByUser_WithDotSegments_ThrowsNotFoundException(string storageKey, string userId, string category)
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser(storageKey, userId, category);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithBackslash_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes\\user-1\\x.pdf", "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenUserIdSegmentIsOnlyAPrefixMatch_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes/user-12/x.pdf", "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenCategoryDoesNotMatch_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("video-intros/user-1/x.mp4", "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenUserIdBelongsToSomeoneElse_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes/other-user/x.pdf", "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithEmptyStorageKey_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser(string.Empty, "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithTooFewSegments_ThrowsNotFoundException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes", "user-1", "resumes");

        act.Should().Throw<NotFoundException>();
    }

    [Theory]
    [InlineData("user/1")]
    [InlineData("user\\1")]
    [InlineData(".")]
    [InlineData("..")]
    public void BuildStorageKey_WithSlashBackslashOrDotSegmentUserId_ThrowsBadRequestException(string userId)
    {
        var act = () => TalentMediaStorageKeyGuard.BuildStorageKey("resumes", userId, "resume.pdf");

        act.Should().Throw<BadRequestException>();
    }

    [Theory]
    [InlineData("notes\\v2.pdf", "v2.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("a/b/c.pdf", "c.pdf")]
    [InlineData("a/b\\c.pdf", "c.pdf")]
    public void BuildStorageKey_SanitizesFileNameSegment_DeterministicallyAcrossOperatingSystems(string fileName, string expectedFileNameSegment)
    {
        var storageKey = TalentMediaStorageKeyGuard.BuildStorageKey("resumes", "user-1", fileName);

        storageKey.Should().Be($"resumes/user-1/{expectedFileNameSegment}");
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("other-user")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public void BuildStorageKey_ThenEnsureOwnedByUser_RoundTripsWithoutThrowing(string userId)
    {
        var storageKey = TalentMediaStorageKeyGuard.BuildStorageKey("resumes", userId, "resume.pdf");

        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser(storageKey, userId, "resumes");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void BuildStorageKey_NeverProducesAKeyThatEnsureOwnedByUserWouldReject_ForDotSegmentUserIds(string userId)
    {
        // BuildStorageKey rejects these userIds outright (BadRequestException) rather than
        // producing a key like "resumes/../{file}" that EnsureOwnedByUser would then reject
        // for the legitimate owner — this is the invariant that must never diverge again.
        var act = () => TalentMediaStorageKeyGuard.BuildStorageKey("resumes", userId, "resume.pdf");

        act.Should().Throw<BadRequestException>();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/.")]
    [InlineData("a/..")]
    public void BuildStorageKey_WhenFileNameSanitizesToDotOrDotDot_ThrowsBadRequestException(string fileName)
    {
        // Same invariant as the userId dot-segment guard: BuildStorageKey must reject a
        // fileName that sanitizes down to "." or ".." rather than producing a key like
        // "resumes/user-1/.." that EnsureOwnedByUser would then reject for the owner.
        var act = () => TalentMediaStorageKeyGuard.BuildStorageKey("resumes", "user-1", fileName);

        act.Should().Throw<BadRequestException>();
    }

    [Theory]
    [InlineData("my_cv.pdf", "my_cv.pdf")]
    [InlineData("resume.pdf", "resume.pdf")]
    [InlineData("cv.docx", "cv.docx")]
    public void ExtractOriginalFileName_WithPlainFileNameWithoutGuidPrefix_ReturnsPlainFileName(string input, string expected)
    {
        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractOriginalFileName_WithLeadingGuidPrefix_StripsGuidPrefixAndUnderscore()
    {
        var guid = Guid.NewGuid();
        var input = $"{guid}_john_doe_resume.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("john_doe_resume.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithStorageKeyPathFolders_StripsPathAndGuidPrefix()
    {
        var guid = Guid.NewGuid();
        var input = $"resumes/pending/{guid}_jane_resume.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("jane_resume.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithMultipleFoldersAndMultipleUnderscores_ExtractsOriginalNameCorrectly()
    {
        var guid = Guid.NewGuid();
        var input = $"job-applications/user-1/{guid}_senior_software_engineer_cv.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("senior_software_engineer_cv.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithWindowsBackslashesInPath_ExtractsOriginalNameCorrectly()
    {
        var guid = Guid.NewGuid();
        var input = $"job-applications\\user-1\\{guid}_jessica_cv.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("jessica_cv.pdf");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractOriginalFileName_WithNullEmptyOrWhitespace_ReturnsDefaultFallback(string? input)
    {
        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("Resume.pdf");
    }

    [Theory]
    [InlineData(null, "Document.pdf")]
    [InlineData("", "CustomFallback.docx")]
    [InlineData("   ", "Attachment.pdf")]
    public void ExtractOriginalFileName_WithCustomFallback_ReturnsCustomFallbackWhenInputEmpty(string? input, string fallback)
    {
        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input, fallback);

        result.Should().Be(fallback);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("folder/.")]
    [InlineData("folder/..")]
    [InlineData("folder\\..")]
    public void ExtractOriginalFileName_WithDotSegments_ReturnsFallback(string input)
    {
        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("Resume.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithInvalidGuidPrefixBeforeUnderscore_ReturnsFullSegment()
    {
        var input = "notaguid_file.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("notaguid_file.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithBareGuidFileNameWithoutUnderscore_ReturnsFullSegment()
    {
        var guid = Guid.NewGuid();
        var input = $"{guid}.pdf";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be($"{guid}.pdf");
    }

    [Fact]
    public void ExtractOriginalFileName_WithGuidAndTrailingUnderscoreOnly_ReturnsFallback()
    {
        var guid = Guid.NewGuid();
        var input = $"{guid}_";

        var result = TalentMediaStorageKeyGuard.ExtractOriginalFileName(input);

        result.Should().Be("Resume.pdf");
    }
}
