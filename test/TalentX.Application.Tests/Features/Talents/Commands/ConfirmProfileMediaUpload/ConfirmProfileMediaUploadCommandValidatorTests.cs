using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ConfirmProfileMediaUpload;

public class ConfirmProfileMediaUploadCommandValidatorTests
{
    private readonly ConfirmProfileMediaUploadCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyStorageKey_HasError()
    {
        var result = _validator.Validate(new ConfirmProfileMediaUploadCommand(string.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidStorageKey_Passes()
    {
        var result = _validator.Validate(new ConfirmProfileMediaUploadCommand("profile-pictures/user-1/new.jpg"));

        result.IsValid.Should().BeTrue();
    }
}
