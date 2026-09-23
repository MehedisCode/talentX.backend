using FluentValidation.TestHelper;
using TalentX.Application.Features.Auth.DeleteAccount;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.DeleteAccount;

public class DeleteAccountCommandValidatorTests
{
    private readonly DeleteAccountCommandValidator _validator = new();

    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    [InlineData("999999")]
    public void Validate_WithSixDigitOtp_ShouldPass(string otp)
    {
        var result = _validator.TestValidate(new DeleteAccountCommand(otp));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData("12 456")]
    public void Validate_WithInvalidOtp_ShouldFail(string otp)
    {
        var result = _validator.TestValidate(new DeleteAccountCommand(otp));
        result.ShouldHaveValidationErrorFor(c => c.Otp);
    }
}
