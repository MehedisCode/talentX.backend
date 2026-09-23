using FluentValidation;

namespace TalentX.Application.Features.JobReports.Commands.DismissJobReport;

public sealed class DismissJobReportCommandValidator : AbstractValidator<DismissJobReportCommand>
{
    public DismissJobReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty();

        RuleFor(x => x.AdminNotes)
            .MaximumLength(1000);
    }
}
