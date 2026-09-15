using FluentValidation.TestHelper;
using TalentX.Application.Features.Users.Queries.GetUsers;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var query = new GetUsersQuery();

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(UserRole.Talent)]
    [InlineData(UserRole.Employer)]
    [InlineData(UserRole.Guest)]
    public void Validate_WithNonAdminRoleFilter_HasNoValidationErrors(UserRole role)
    {
        var query = new GetUsersQuery { Role = role };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRoleFilterIsAdmin_HasValidationError()
    {
        var query = new GetUsersQuery { Role = UserRole.Admin };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Role)
            .WithErrorMessage("Role filter cannot be Admin.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WhenPageIsLessThanOne_HasValidationError(int invalidPage)
    {
        var query = new GetUsersQuery { Page = invalidPage };

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
        var query = new GetUsersQuery { PageSize = invalidPageSize };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize must be at least 1.");
    }
}
