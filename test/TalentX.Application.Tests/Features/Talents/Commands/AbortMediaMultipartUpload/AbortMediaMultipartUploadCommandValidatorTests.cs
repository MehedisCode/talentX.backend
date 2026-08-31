using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.AbortMediaMultipartUpload;

public class AbortMediaMultipartUploadCommandValidatorTests
{
    private readonly AbortMediaMultipartUploadCommandValidator _validator = new();

    [Fact]
    public void Validate_WithMissingUploadId_HasError()
    {
        var result = _validator.Validate(new AbortMediaMultipartUploadCommand("video-intros/user-1/x.mp4", string.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new AbortMediaMultipartUploadCommand("video-intros/user-1/x.mp4", "upload-1"));

        result.IsValid.Should().BeTrue();
    }
}
