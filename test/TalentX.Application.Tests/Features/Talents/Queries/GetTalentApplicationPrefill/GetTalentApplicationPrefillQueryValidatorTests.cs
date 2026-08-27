using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentApplicationPrefill;

public class GetTalentApplicationPrefillQueryValidatorTests
{
    private readonly GetTalentApplicationPrefillQueryValidator _validator = new();

    [Fact]
    public void Validate_WithAValidUserId_HasNoErrors()
    {
        var result = _validator.TestValidate(new GetTalentApplicationPrefillQuery("user-1"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingUserId_HasError(string? userId)
    {
        var result = _validator.TestValidate(new GetTalentApplicationPrefillQuery(userId!));

        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }
}
