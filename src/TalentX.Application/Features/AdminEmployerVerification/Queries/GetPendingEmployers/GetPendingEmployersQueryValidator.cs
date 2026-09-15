using FluentValidation;

namespace TalentX.Application.Features.AdminEmployerVerification.Queries.GetPendingEmployers;

public sealed class GetPendingEmployersQueryValidator : AbstractValidator<GetPendingEmployersQuery>
{
    public GetPendingEmployersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
    }
}
