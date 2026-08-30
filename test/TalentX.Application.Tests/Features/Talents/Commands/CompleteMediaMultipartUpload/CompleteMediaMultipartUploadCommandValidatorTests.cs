using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.CompleteMediaMultipartUpload;

public class CompleteMediaMultipartUploadCommandValidatorTests
{
    private readonly CompleteMediaMultipartUploadCommandValidator _validator = new();

    [Fact]
    public void Validate_WithNoParts_HasError()
    {
        var command = new CompleteMediaMultipartUploadCommand("video-intros/user-1/x.mp4", "upload-1", []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidParts_Passes()
    {
        var command = new CompleteMediaMultipartUploadCommand(
            "video-intros/user-1/x.mp4",
            "upload-1",
            [new CompleteMediaMultipartUploadPartItem(1, "\"etag-1\"")]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
