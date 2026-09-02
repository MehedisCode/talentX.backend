using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Queries.GetPublishedJobs;

public class GetPublishedJobsQueryValidatorTests
{
    private readonly GetPublishedJobsQueryValidator _validator = new();

    private static GetPublishedJobsQuery ValidQuery() => new(
        Keyword: "software",
        Location: "Dhaka",
        EmploymentType: EmploymentType.FullTime,
        ExperienceLevel: ExperienceLevel.MidLevel,
        WorkArrangement: WorkArrangement.FullRemote,
        Page: 1,
        PageSize: 20
    );

    [Fact]
    public void Validate_WithValidQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(ValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var query = new GetPublishedJobsQuery();

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_WithPageLessThanOne_HasValidationError(int invalidPage)
    {
        var query = ValidQuery() with { Page = invalidPage };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(200)]
    public void Validate_WithPageSizeOutOfRange_HasValidationError(int invalidPageSize)
    {
        var query = ValidQuery() with { PageSize = invalidPageSize };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 100.");
    }

    [Fact]
    public void Validate_WithKeywordOver100Characters_HasValidationError()
    {
        var query = ValidQuery() with { Keyword = new string('a', 101) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Keyword)
            .WithErrorMessage("Keyword must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithKeyword100Characters_HasNoValidationError()
    {
        var query = ValidQuery() with { Keyword = new string('a', 100) };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.Keyword);
    }

    [Fact]
    public void Validate_WithLocationOver100Characters_HasValidationError()
    {
        var query = ValidQuery() with { Location = new string('a', 101) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Location)
            .WithErrorMessage("Location must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithLocation100Characters_HasNoValidationError()
    {
        var query = ValidQuery() with { Location = new string('a', 100) };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(q => q.Location);
    }
}
