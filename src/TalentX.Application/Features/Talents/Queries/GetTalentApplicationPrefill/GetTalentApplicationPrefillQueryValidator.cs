using FluentValidation;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;

public sealed class GetTalentApplicationPrefillQueryValidator : AbstractValidator<GetTalentApplicationPrefillQuery>
{
    public GetTalentApplicationPrefillQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
