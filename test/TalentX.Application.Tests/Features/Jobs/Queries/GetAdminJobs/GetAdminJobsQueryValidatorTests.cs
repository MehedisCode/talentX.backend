using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Queries.GetAdminJobs;

public sealed class GetAdminJobsQueryValidatorTests
{
    private readonly GetAdminJobsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(JobStatus.Published)]
    [InlineData(JobStatus.Deactivated)]
    [InlineData(JobStatus.PendingReview)]
    public void Validate_WithModeratedStatusFilter_HasNoValidationErrors(JobStatus status)
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { Status = status });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(JobStatus.Draft)]
    [InlineData(JobStatus.Closed)]
    public void Validate_WithNonModeratedStatusFilter_HasValidationError(JobStatus status)
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { Status = status });

        result.ShouldHaveValidationErrorFor(query => query.Status!.Value)
            .WithErrorMessage("Status filter must be Published, Deactivated or PendingReview.");
    }

    [Fact]
    public void Validate_WhenSearchQueryExceeds200Characters_HasValidationError()
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { SearchQuery = new string('a', 201) });

        result.ShouldHaveValidationErrorFor(query => query.SearchQuery)
            .WithErrorMessage("Search query must be 200 characters or fewer.");
    }

    [Fact]
    public void Validate_WhenSearchQueryIs200Characters_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { SearchQuery = new string('a', 200) });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsLessThanOne_HasValidationError(int invalidPage)
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { Page = invalidPage });

        result.ShouldHaveValidationErrorFor(query => query.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenPageSizeIsLessThanOne_HasValidationError(int invalidPageSize)
    {
        var result = _validator.TestValidate(new GetAdminJobsQuery { PageSize = invalidPageSize });

        result.ShouldHaveValidationErrorFor(query => query.PageSize)
            .WithErrorMessage("PageSize must be at least 1.");
    }
}
