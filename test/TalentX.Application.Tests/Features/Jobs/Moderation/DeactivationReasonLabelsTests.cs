using FluentAssertions;
using TalentX.Application.Features.Jobs.Moderation;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Moderation;

public sealed class DeactivationReasonLabelsTests
{
    [Theory]
    [InlineData(DeactivationReason.Spam, "Spam")]
    [InlineData(DeactivationReason.InappropriateContent, "Inappropriate content")]
    [InlineData(DeactivationReason.MisleadingInformation, "Misleading or invalid information")]
    [InlineData(DeactivationReason.Discriminatory, "Discriminatory content")]
    [InlineData(DeactivationReason.DuplicatePosting, "Duplicate posting")]
    [InlineData(DeactivationReason.Outdated, "Outdated job")]
    [InlineData(DeactivationReason.Other, "Other")]
    public void ToLabel_ReturnsReadableLabelForEachReason(DeactivationReason reason, string expectedLabel)
    {
        DeactivationReasonLabels.ToLabel(reason).Should().Be(expectedLabel);
    }

    [Fact]
    public void ToLabel_CoversEveryEnumValueWithNonEmptyText()
    {
        // Guards against adding a reason later and forgetting its label.
        foreach (var reason in Enum.GetValues<DeactivationReason>())
        {
            DeactivationReasonLabels.ToLabel(reason).Should().NotBeNullOrWhiteSpace();
        }
    }
}
