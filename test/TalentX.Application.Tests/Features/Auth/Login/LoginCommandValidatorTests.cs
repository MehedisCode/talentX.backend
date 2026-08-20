using FluentAssertions;
using TalentX.Application.Features.Auth.Login;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.Login;

/// <summary>
/// Unit tests for LoginCommandValidator
/// Tests cover validation rules for Email and Password fields
/// </summary>
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    #region Email Validation Tests

    [Theory]
    [InlineData("valid.email@example.com")]
    [InlineData("user@domain.co.uk")]
    [InlineData("test123@test-domain.com")]
    public void Validate_WithValidEmail_ShouldSucceed(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Where(e => e.PropertyName == "Email").Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("", "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Email" && 
            e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user name@example.com")]
    [InlineData("user@.com")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Email" && 
            e.ErrorMessage.Contains("valid email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithEmailExceedingMaxLength_ShouldFail()
    {
        // Arrange
        // Create an email that exceeds 256 characters
        var longEmail = new string('a', 250) + "@example.com"; // 264 characters total
        var command = new LoginCommand(longEmail, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Email" && 
            e.ErrorMessage.Contains("256", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithEmailAtMaxLength_ShouldSucceed()
    {
        // Arrange
        // Create a valid email whose total length is exactly 256 characters.
        var email = new string('a', 244) + "@example.com";
        email.Length.Should().Be(256);
        var command = new LoginCommand(email, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullEmail_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(null!, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Password Validation Tests

    [Theory]
    [InlineData("ValidPassword123!")]
    [InlineData("AnotherValid@Pass99")]
    [InlineData("TestPassword#2024")]
    public void Validate_WithValidPassword_ShouldSucceed(string password)
    {
        // Arrange
        var command = new LoginCommand("valid@example.com", password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Where(e => e.PropertyName == "Password").Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("valid@example.com", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Password" && 
            e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithPasswordExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var longPassword = new string('a', 257); // Exceeds 256
        var command = new LoginCommand("valid@example.com", longPassword);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == "Password" && 
            e.ErrorMessage.Contains("256", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithPasswordAtMaxLength_ShouldSucceed()
    {
        // Arrange
        var maxPassword = new string('a', 256);
        var command = new LoginCommand("valid@example.com", maxPassword);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("valid@example.com", null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Combined Validation Tests

    [Fact]
    public void Validate_WithBothEmailAndPasswordEmpty_ShouldFailWithMultipleErrors()
    {
        // Arrange
        var command = new LoginCommand("", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithValidEmailAndInvalidPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("valid@example.com", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
        result.Errors.Should().NotContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmailAndValidPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand("invalid-email", "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().NotContain(e => e.PropertyName == "Password");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("a@b.c")]
    [InlineData("test@localhost")]
    [InlineData("user+tag@example.com")]
    public void Validate_WithEdgeCaseValidEmails_ShouldSucceed(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "ValidPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithWhitespacePassword_ShouldSucceed()
    {
        // Arrange - whitespace alone is technically not empty in the context of string validation
        var command = new LoginCommand("valid@example.com", "   ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        // Whitespace passes the empty check but should succeed based on the validator rules
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
