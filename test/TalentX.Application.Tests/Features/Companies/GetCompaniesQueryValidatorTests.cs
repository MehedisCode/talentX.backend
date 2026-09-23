using FluentValidation.TestHelper;
using TalentX.Application.Features.Companies.Queries.GetCompanies;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class GetCompaniesQueryValidatorTests
{
    private readonly GetCompaniesQueryValidator _validator = new();

    private static GetCompaniesQuery ValidQuery() => new(
        Keyword: "acme",
        Industry: "Software",
        Location: "Dhaka",
        CompanySize: "11-50",
        SortBy: "name",
        SortDir: "asc",
        Page: 1,
        PageSize: 10
    );


    [Theory]
    [InlineData("name")]
    [InlineData("openings")]
    [InlineData("vacancies")]
    [InlineData("newest")]
    [InlineData("NAME")]
    public void Validate_WithAllowedSortBy_HasNoValidationError(string sortBy)
    {
        var query = ValidQuery() with { SortBy = sortBy };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.SortBy);
    }

    [Fact]
    public void Validate_WithUnknownSortBy_HasValidationError()
    {
        var query = ValidQuery() with { SortBy = "founded" };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.SortBy)
            .WithErrorMessage("SortBy must be one of: name, openings, vacancies, newest.");
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    [InlineData("ASC")]
    public void Validate_WithAllowedSortDir_HasNoValidationError(string sortDir)
    {
        var query = ValidQuery() with { SortDir = sortDir };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.SortDir);
    }

    [Fact]
    public void Validate_WithUnknownSortDir_HasValidationError()
    {
        var query = ValidQuery() with { SortDir = "sideways" };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.SortDir)
            .WithErrorMessage("SortDir must be one of: asc, desc.");
    }
}
