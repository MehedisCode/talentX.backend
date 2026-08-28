using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Queries.GetTalentProfile;

public class GetTalentProfileQueryValidatorTests
{
    private readonly GetTalentProfileQueryValidator _validator = new();

    [Fact]
    public void Validate_HasNoErrors()
    {
        var result = _validator.TestValidate(new GetTalentProfileQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
