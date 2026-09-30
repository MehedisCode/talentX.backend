using FluentValidation.TestHelper;
using TalentX.Application.Features.JobApplications.Queries.GetApplicationStatusHistory;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Queries.GetApplicationStatusHistory;

public class GetApplicationStatusHistoryQueryValidatorTests
{
    private readonly GetApplicationStatusHistoryQueryValidator _validator = new();

    [Fact]
    public void Validate_EmptyApplicationId_ShouldHaveValidationError()
    {
        var result = _validator.TestValidate(new GetApplicationStatusHistoryQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Fact]
    public void Validate_ValidApplicationId_ShouldNotHaveValidationError()
    {
        var result = _validator.TestValidate(new GetApplicationStatusHistoryQuery(Guid.NewGuid()));

        result.ShouldNotHaveValidationErrorFor(x => x.ApplicationId);
    }
}
