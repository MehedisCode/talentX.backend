using FluentValidation.TestHelper;
using TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers.Queries;

public sealed class GetEmployerDashboardQueryValidatorTests
{
    private readonly GetEmployerDashboardQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var query = new GetEmployerDashboardQuery();

        var validationResult = _validator.TestValidate(query);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }
}
