using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentApplicationPrefill;

public class GetTalentApplicationPrefillQueryValidatorTests
{
    private readonly GetTalentApplicationPrefillQueryValidator _validator = new();

    [Fact]
    public void Validate_HasNoErrors()
    {
        var result = _validator.TestValidate(new GetTalentApplicationPrefillQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
