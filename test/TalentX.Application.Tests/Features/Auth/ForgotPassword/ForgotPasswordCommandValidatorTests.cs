using FluentAssertions;
using TalentX.Application.Features.Auth.ForgotPassword;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ForgotPasswordCommand("user@example.com");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenEmailIsEmpty_ShouldHaveValidationError(string? emptyEmail)
    {
        var command = new ForgotPasswordCommand(emptyEmail!);

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ForgotPasswordCommand.Email) &&
            failure.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.com")]
    [InlineData("user@")]
    public void Validate_WhenEmailIsInvalidFormat_ShouldHaveValidationError(string invalidEmail)
    {
        var command = new ForgotPasswordCommand(invalidEmail);

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ForgotPasswordCommand.Email) &&
            failure.ErrorMessage == "A valid email address is required.");
    }

    [Fact]
    public void Validate_WhenEmailExceedsMaxLength_ShouldHaveValidationError()
    {
        var excessiveEmail = $"{new string('a', 250)}@example.com";
        var command = new ForgotPasswordCommand(excessiveEmail);

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ForgotPasswordCommand.Email) &&
            failure.ErrorMessage == "Email must not exceed 256 characters.");
    }
}
