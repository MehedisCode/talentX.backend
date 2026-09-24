using FluentValidation;

namespace TalentX.Application.Features.JobReports.Commands.ResolveJobReport;

public sealed class ResolveJobReportCommandValidator : AbstractValidator<ResolveJobReportCommand>
{
    public ResolveJobReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty();

        RuleFor(x => x.DeactivationReason)
            .IsInEnum();

        RuleFor(x => x.Details)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(1000);

        RuleFor(x => x.AdminNotes)
            .MaximumLength(1000);
    }
}
