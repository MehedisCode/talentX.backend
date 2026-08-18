using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using Xunit;

namespace TalentX.Tests.Employers;

public sealed class CreateEmployerCommandValidatorTests
{
    private readonly CreateEmployerCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldBeValid_WhenAllFieldsAreValid()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "jane@example.com",
            "Tech Corp",
            "SecureP@ssw0rd!",
            "SecureP@ssw0rd!"
        );

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("invalid@")]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid(string email)
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            email,
            "Tech Corp",
            "SecureP@ssw0rd!",
            "SecureP@ssw0rd!"
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEmployerCommand.Email));
    }

    [Theory]
    [InlineData("short1!")] // < 8 chars
    [InlineData("nouppercase123!")] // no uppercase
    [InlineData("NOLOWERCASE123!")] // no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecialChar123")] // no special char
    public void Validate_ShouldHaveError_WhenPasswordDoesNotMeetComplexity(string password)
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "jane@example.com",
            "Tech Corp",
            password,
            password
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEmployerCommand.Password));
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordsDoNotMatch()
    {
        var command = new CreateEmployerCommand(
            "Jane Doe",
            "jane@example.com",
            "Tech Corp",
            "SecureP@ssw0rd!",
            "DifferentP@ssw0rd!"
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEmployerCommand.ConfirmPassword));
    }

    [Theory]
    [InlineData("", "Tech Corp")]
    [InlineData("Jane Doe", "")]
    public void Validate_ShouldHaveError_WhenRequiredNamesAreEmpty(string name, string companyName)
    {
        var command = new CreateEmployerCommand(
            name,
            "jane@example.com",
            companyName,
            "SecureP@ssw0rd!",
            "SecureP@ssw0rd!"
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
