using FluentValidation.TestHelper;
using TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;
using Xunit;

namespace TalentX.Application.Tests.Features.AuditLogs.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryValidatorTests
{
    private readonly GetAuditLogsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var query = new GetAuditLogsQuery();

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidDatesAndPagination_HasNoValidationErrors()
    {
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 15, 23, 59, 59, DateTimeKind.Utc);
        var query = new GetAuditLogsQuery
        {
            SearchQuery = "user@talentx.com",
            StartDate = startDate,
            EndDate = endDate,
            Page = 2,
            PageSize = 10
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateEqualsEndDate_HasNoValidationErrors()
    {
        var date = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var query = new GetAuditLogsQuery
        {
            StartDate = date,
            EndDate = date
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateOnlyProvided_HasNoValidationErrors()
    {
        var query = new GetAuditLogsQuery { StartDate = DateTime.UtcNow.AddDays(-7) };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenEndDateOnlyProvided_HasNoValidationErrors()
    {
        var query = new GetAuditLogsQuery { EndDate = DateTime.UtcNow };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateIsAfterEndDate_HasValidationError()
    {
        var query = new GetAuditLogsQuery
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("StartDate must be earlier than or equal to EndDate.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WhenPageIsLessThanOne_HasValidationError(int invalidPage)
    {
        var query = new GetAuditLogsQuery { Page = invalidPage };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_WhenPageSizeIsLessThanOne_HasValidationError(int invalidPageSize)
    {
        var query = new GetAuditLogsQuery { PageSize = invalidPageSize };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize must be at least 1.");
    }
}
