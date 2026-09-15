using FluentValidation;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize must be at least 1.");

        RuleFor(x => x.Role)
            .NotEqual(UserRole.Admin)
            .When(x => x.Role.HasValue)
            .WithMessage("Role filter cannot be Admin.");
    }
}
