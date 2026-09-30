using FluentValidation.TestHelper;
using TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;
using Xunit;

namespace TalentX.Application.Tests.Features.AdminDashboard.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryValidatorTests
{
    private readonly GetAdminDashboardQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new GetAdminDashboardQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidRange_HasNoValidationErrors()
    {
        var query = new GetAdminDashboardQuery
        {
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 24, 23, 59, 59, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateEqualsEndDate_HasNoValidationErrors()
    {
        var instant = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        var result = _validator.TestValidate(new GetAdminDashboardQuery { StartDate = instant, EndDate = instant });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyOneBoundIsProvided_HasNoValidationErrors()
    {
        var onlyStart = _validator.TestValidate(new GetAdminDashboardQuery { StartDate = DateTime.UtcNow.AddDays(-7) });
        var onlyEnd = _validator.TestValidate(new GetAdminDashboardQuery { EndDate = DateTime.UtcNow });

        onlyStart.ShouldNotHaveAnyValidationErrors();
        onlyEnd.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateIsAfterEndDate_HasValidationError()
    {
        var query = new GetAdminDashboardQuery
        {
            StartDate = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("StartDate must be earlier than or equal to EndDate.");
    }

    [Fact]
    public void Validate_WhenRangeIsExactlyMaxDays_HasNoValidationErrors()
    {
        var start = new DateTime(2025, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetAdminDashboardQuery { StartDate = start, EndDate = start.AddDays(366) };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRangeExceedsMaxDays_HasValidationError()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetAdminDashboardQuery { StartDate = start, EndDate = start.AddDays(367) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("The date range must not exceed 366 days.");
    }
}
