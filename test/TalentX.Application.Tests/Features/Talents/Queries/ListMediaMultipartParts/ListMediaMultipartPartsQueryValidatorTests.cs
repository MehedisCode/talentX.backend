using FluentAssertions;
using TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.ListMediaMultipartParts;

public class ListMediaMultipartPartsQueryValidatorTests
{
    private readonly ListMediaMultipartPartsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithMissingStorageKey_HasError()
    {
        var result = _validator.Validate(new ListMediaMultipartPartsQuery(string.Empty, "upload-1"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var result = _validator.Validate(new ListMediaMultipartPartsQuery("video-intros/user-1/x.mp4", "upload-1"));

        result.IsValid.Should().BeTrue();
    }
}
