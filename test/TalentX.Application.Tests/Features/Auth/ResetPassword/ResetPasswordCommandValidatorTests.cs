using FluentAssertions;
using TalentX.Application.Features.Auth.ResetPassword;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            "valid-reset-token",
            "ValidPassword123!",
            "ValidPassword123!");

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
        var command = new ResetPasswordCommand(
            emptyEmail!,
            "valid-reset-token",
            "ValidPassword123!",
            "ValidPassword123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ResetPasswordCommand.Email) &&
            failure.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveValidationError(string invalidEmail)
    {
        var command = new ResetPasswordCommand(
            invalidEmail,
            "valid-reset-token",
            "ValidPassword123!",
            "ValidPassword123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ResetPasswordCommand.Email) &&
            failure.ErrorMessage == "A valid email address is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenTokenIsEmpty_ShouldHaveValidationError(string? emptyToken)
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            emptyToken!,
            "ValidPassword123!",
            "ValidPassword123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ResetPasswordCommand.Token) &&
            failure.ErrorMessage == "Reset token is required.");
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("1234567")]
    public void Validate_WhenNewPasswordIsTooShort_ShouldHaveValidationError(string shortPassword)
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            "valid-reset-token",
            shortPassword,
            shortPassword);

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenNewPasswordIsMissingUppercase_ShouldHaveValidationError()
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            "valid-reset-token",
            "lowercase123!",
            "lowercase123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenNewPasswordIsMissingSpecialCharacter_ShouldHaveValidationError()
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            "valid-reset-token",
            "NoSpecialChar123",
            "NoSpecialChar123");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenPasswordsDoNotMatch_ShouldHaveValidationError()
    {
        var command = new ResetPasswordCommand(
            "user@example.com",
            "valid-reset-token",
            "ValidPassword123!",
            "DifferentPassword123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ResetPasswordCommand.ConfirmPassword) &&
            failure.ErrorMessage == "Passwords do not match.");
    }
}
