using FluentValidation;

namespace TalentX.Application.Features.Invitations.Commands.SendJobInvitation;

public sealed class SendJobInvitationCommandValidator : AbstractValidator<SendJobInvitationCommand>
{
    public SendJobInvitationCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");

        RuleFor(x => x.TalentUserId)
            .NotEmpty()
            .WithMessage("Talent user ID is required.");
    }
}
