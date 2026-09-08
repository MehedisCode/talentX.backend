using FluentValidation;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;

public sealed class SendBatchInterviewInvitationsCommandValidator : AbstractValidator<SendBatchInterviewInvitationsCommand>
{
    public SendBatchInterviewInvitationsCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");

        RuleFor(x => x.ApplicationIds)
            .NotEmpty().WithMessage("At least one candidate application must be selected.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid interview type.");

        RuleFor(x => x.AssessmentUrl)
            .NotEmpty().WithMessage("Assessment URL is required for online assessments.")
            .When(x => x.Type == InterviewType.OnlineAssessment);

        RuleFor(x => x.LocationOrLink)
            .NotEmpty().WithMessage("Meeting link or office address is required.")
            .When(x => x.Type is InterviewType.OnCall or InterviewType.OnSite);
    }
}
