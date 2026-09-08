using FluentValidation;

namespace TalentX.Application.Features.Invitations.Queries.GetJobInvitations;

public sealed class GetJobInvitationsQueryValidator : AbstractValidator<GetJobInvitationsQuery>
{
    public GetJobInvitationsQueryValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}
