using FluentValidation;
using TalentX.Application.Features.JobReports.Constants;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Commands.ReportJob;

public sealed class ReportJobCommandValidator : AbstractValidator<ReportJobCommand>
{
    public ReportJobCommandValidator()
    {
        RuleFor(command => command.JobId)
            .NotEmpty().WithMessage("Job ID is required.");

        RuleFor(command => command.Reason)
            .IsInEnum().WithMessage("A valid report reason is required.");

        RuleFor(command => command.AdditionalDetails)
            .MaximumLength(JobReportConstants.MaxAdditionalDetailsLength)
            .WithMessage($"Additional details cannot exceed {JobReportConstants.MaxAdditionalDetailsLength} characters.");

        When(command => command.Reason == JobReportReason.Other, () =>
        {
            RuleFor(command => command.AdditionalDetails)
                .NotEmpty().WithMessage("Please provide details when selecting 'Other'.")
                .MinimumLength(JobReportConstants.MinAdditionalDetailsLength).WithMessage($"Details must be at least {JobReportConstants.MinAdditionalDetailsLength} characters.");
        });

        When(command => command.Reason != JobReportReason.Other && !string.IsNullOrWhiteSpace(command.AdditionalDetails), () =>
        {
            RuleFor(command => command.AdditionalDetails)
                .MinimumLength(JobReportConstants.MinAdditionalDetailsLength).WithMessage($"Details must be at least {JobReportConstants.MinAdditionalDetailsLength} characters.");
        });
    }
}
