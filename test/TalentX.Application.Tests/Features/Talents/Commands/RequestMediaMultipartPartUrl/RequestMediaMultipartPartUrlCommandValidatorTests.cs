using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.RequestMediaMultipartPartUrl;

public class RequestMediaMultipartPartUrlCommandValidatorTests
{
    private readonly RequestMediaMultipartPartUrlCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validate_WhenPartNumberIsOutOfRange_HasError(int partNumber)
    {
        var result = _validator.Validate(new RequestMediaMultipartPartUrlCommand("video-intros/user-1/x.mp4", "upload-1", partNumber));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Validate_WhenPartNumberIsInRange_Passes(int partNumber)
    {
        var result = _validator.Validate(new RequestMediaMultipartPartUrlCommand("video-intros/user-1/x.mp4", "upload-1", partNumber));

        result.IsValid.Should().BeTrue();
    }
}
