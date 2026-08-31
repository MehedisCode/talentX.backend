using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.InitiateMediaMultipartUpload;

public class InitiateMediaMultipartUploadCommandValidatorTests
{
    private readonly InitiateMediaMultipartUploadCommandValidator _validator = new();

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    [InlineData("video/quicktime")]
    public void Validate_WithAllowedContentType_Passes(string contentType)
    {
        var result = _validator.Validate(new InitiateMediaMultipartUploadCommand(contentType));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDisallowedContentType_HasError()
    {
        var result = _validator.Validate(new InitiateMediaMultipartUploadCommand("video/avi"));

        result.IsValid.Should().BeFalse();
    }
}
