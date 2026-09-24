using FluentValidation;
using TalentX.Application.Features.AdminDashboard.Constants;

namespace TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryValidator : AbstractValidator<GetAdminDashboardQuery>
{
    public GetAdminDashboardQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage("StartDate must be earlier than or equal to EndDate.");

        // Only checked when both dates are present and in order, so a reversed range reports just one error.
        RuleFor(x => x)
            .Must(x => (x.EndDate!.Value - x.StartDate!.Value).TotalDays <= AdminDashboardConstants.MaxRangeDays)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue && x.StartDate <= x.EndDate)
            .WithMessage($"The date range must not exceed {AdminDashboardConstants.MaxRangeDays} days.");
    }
}
