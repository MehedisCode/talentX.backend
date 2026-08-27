using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentProfile;

public class GetTalentProfileQueryValidatorTests
{
    private readonly GetTalentProfileQueryValidator _validator = new();

    [Fact]
    public void Validate_WithAValidUserId_HasNoErrors()
    {
        var result = _validator.TestValidate(new GetTalentProfileQuery("user-1"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingUserId_HasError(string? userId)
    {
        var result = _validator.TestValidate(new GetTalentProfileQuery(userId!));

        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }
}
