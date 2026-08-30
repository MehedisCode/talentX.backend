using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.RequestProfileMediaUploadUrl;

public class RequestProfileMediaUploadUrlCommandValidatorTests
{
    private readonly RequestProfileMediaUploadUrlCommandValidator _validator = new();

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_WithAllowedContentType_Passes(string contentType)
    {
        var result = _validator.Validate(new RequestProfileMediaUploadUrlCommand(contentType));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDisallowedContentType_HasError()
    {
        var result = _validator.Validate(new RequestProfileMediaUploadUrlCommand("image/gif"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyContentType_HasError()
    {
        var result = _validator.Validate(new RequestProfileMediaUploadUrlCommand(string.Empty));

        result.IsValid.Should().BeFalse();
    }
}
