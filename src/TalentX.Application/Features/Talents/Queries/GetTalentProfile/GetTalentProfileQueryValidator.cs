using FluentValidation;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfile;

public sealed class GetTalentProfileQueryValidator : AbstractValidator<GetTalentProfileQuery>
{
    public GetTalentProfileQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
