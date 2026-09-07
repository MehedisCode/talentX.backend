using FluentAssertions;
using TalentX.Application.Features.Auth.AddPassword;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.AddPassword;

public sealed class AddPasswordCommandValidatorTests
{
    private readonly AddPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new AddPasswordCommand(
            "ValidPassword123!",
            "ValidPassword123!",
            "valid-refresh-token");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Pass1!")]
    [InlineData("1234567")]
    public void Validate_WhenPasswordIsTooShort_ShouldHaveValidationError(string shortPassword)
    {
        var command = new AddPasswordCommand(
            shortPassword,
            shortPassword);

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(AddPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenPasswordIsMissingUppercase_ShouldHaveValidationError()
    {
        var command = new AddPasswordCommand(
            "lowercase123!",
            "lowercase123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(AddPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenPasswordIsMissingSpecialCharacter_ShouldHaveValidationError()
    {
        var command = new AddPasswordCommand(
            "NoSpecialChar123",
            "NoSpecialChar123");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure => failure.PropertyName == nameof(AddPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WhenPasswordsDoNotMatch_ShouldHaveValidationError()
    {
        var command = new AddPasswordCommand(
            "ValidPassword123!",
            "DifferentPassword123!");

        var validationResult = _validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(AddPasswordCommand.ConfirmPassword) &&
            failure.ErrorMessage == "Passwords do not match.");
    }
}
