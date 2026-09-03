using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Employers.Queries.GetEmployerJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers.Queries;

public sealed class GetEmployerJobsQueryValidatorTests
{
    private readonly GetEmployerJobsQueryValidator _validator = new();

    private static GetEmployerJobsQuery ValidQuery() => new(
        Page: 1,
        PageSize: 10,
        Status: JobStatus.Published,
        IsExpired: false);

    [Fact]
    public void Validate_WithValidQuery_HasNoValidationErrors()
    {
        var validationResult = _validator.TestValidate(ValidQuery());

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var defaultQuery = new GetEmployerJobsQuery();

        var validationResult = _validator.TestValidate(defaultQuery);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WithPageLessThanOne_HasValidationError(int invalidPage)
    {
        var query = ValidQuery() with { Page = invalidPage };

        var validationResult = _validator.TestValidate(query);

        validationResult.ShouldHaveValidationErrorFor(q => q.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(250)]
    public void Validate_WithPageSizeOutOfRange_HasValidationError(int invalidPageSize)
    {
        var query = ValidQuery() with { PageSize = invalidPageSize };

        var validationResult = _validator.TestValidate(query);

        validationResult.ShouldHaveValidationErrorFor(q => q.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 100.");
    }
}
