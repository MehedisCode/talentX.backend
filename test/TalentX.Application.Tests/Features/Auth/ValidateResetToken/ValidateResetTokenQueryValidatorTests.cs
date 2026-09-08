using FluentAssertions;
using TalentX.Application.Features.Auth.ValidateResetToken;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ValidateResetToken;

public sealed class ValidateResetTokenQueryValidatorTests
{
    private readonly ValidateResetTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new ValidateResetTokenQuery("user@example.com", "valid-reset-token");

        var validationResult = _validator.Validate(query);

        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenEmailIsEmpty_ShouldHaveValidationError(string? emptyEmail)
    {
        var query = new ValidateResetTokenQuery(emptyEmail!, "valid-reset-token");

        var validationResult = _validator.Validate(query);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ValidateResetTokenQuery.Email) &&
            failure.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveValidationError(string invalidEmail)
    {
        var query = new ValidateResetTokenQuery(invalidEmail, "valid-reset-token");

        var validationResult = _validator.Validate(query);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ValidateResetTokenQuery.Email) &&
            failure.ErrorMessage == "A valid email address is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenTokenIsEmpty_ShouldHaveValidationError(string? emptyToken)
    {
        var query = new ValidateResetTokenQuery("user@example.com", emptyToken!);

        var validationResult = _validator.Validate(query);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(ValidateResetTokenQuery.Token) &&
            failure.ErrorMessage == "Reset token is required.");
    }
}
