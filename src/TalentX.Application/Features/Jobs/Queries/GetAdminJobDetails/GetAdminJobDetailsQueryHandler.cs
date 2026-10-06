using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Moderation;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Queries.GetAdminJobDetails;

public sealed class GetAdminJobDetailsQueryHandler : IRequestHandler<GetAdminJobDetailsQuery, AdminJobDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminJobDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminJobDetailsDto> Handle(GetAdminJobDetailsQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken);

        // Drafts and closed jobs are not moderated, so they are invisible here rather than
        // returned and then hidden by the UI.
        if (job is null || !ModerationStatuses.Contains(job.Status))
        {
            throw new NotFoundException("Job", request.JobId);
        }

        // Job.EmployerId holds the login user id, which matches User.UserId, not User.Id.
        var employer = await _unitOfWork.Users.GetByUserIdAsync(job.EmployerId);
        var history = await _unitOfWork.JobModerationHistories.GetByJobIdAsync(job.Id, cancellationToken);

        return new AdminJobDetailsDto
        {
            Id = job.Id,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            Requirements = job.Requirements,
            Location = job.Location,
            City = job.City,
            Country = job.Country,
            EmploymentType = job.EmploymentType,
            ExperienceLevel = job.ExperienceLevel,
            WorkArrangement = job.WorkArrangement,
            MinOverlapHours = job.MinOverlapHours,
            RequiredSkills = job.RequiredSkills,
            Vacancies = job.Vacancies,
            SalaryType = job.SalaryType,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            SalaryCurrency = job.SalaryCurrency,
            SalaryPeriod = job.SalaryPeriod,
            Status = job.Status,
            IsExpired = job.Status == JobStatus.Published && job.ExpireAt.Date < DateTime.UtcNow.Date,
            CreatedAt = job.CreatedAt,
            PublishedAt = job.PublishedAt,
            FirstPublishedAt = job.FirstPublishedAt,
            ExpireAt = job.ExpireAt,
            EmployerName = employer?.Name ?? string.Empty,
            EmployerEmail = employer?.Email ?? string.Empty,
            DeactivationReason = job.DeactivationReason,
            DeactivationReasonLabel = job.DeactivationReason.HasValue
                ? DeactivationReasonLabels.ToLabel(job.DeactivationReason.Value)
                : null,
            DeactivationDetails = job.DeactivationDetails,
            DeactivatedAt = job.DeactivatedAt,
            History = history.Select(entry => new JobModerationHistoryDto
            {
                Id = entry.Id,
                Action = entry.Action,
                FromStatus = entry.FromStatus,
                ToStatus = entry.ToStatus,
                Reason = entry.Reason,
                ReasonLabel = entry.Reason.HasValue
                    ? DeactivationReasonLabels.ToLabel(entry.Reason.Value)
                    : null,
                Details = entry.Details,
                ActorEmail = entry.ActorEmail,
                CreatedAt = entry.CreatedAt
            }).ToList()
        };
    }
}
