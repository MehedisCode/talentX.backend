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
    public void EnsureOwnedByUser_WithDotSegments_ThrowsForbiddenException(string storageKey, string userId, string category)
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser(storageKey, userId, category);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithBackslash_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes\\user-1\\x.pdf", "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenUserIdSegmentIsOnlyAPrefixMatch_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes/user-12/x.pdf", "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenCategoryDoesNotMatch_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("video-intros/user-1/x.mp4", "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WhenUserIdBelongsToSomeoneElse_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes/other-user/x.pdf", "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithEmptyStorageKey_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser(string.Empty, "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureOwnedByUser_WithTooFewSegments_ThrowsForbiddenException()
    {
        var act = () => TalentMediaStorageKeyGuard.EnsureOwnedByUser("resumes", "user-1", "resumes");

        act.Should().Throw<ForbiddenException>();
    }
}
