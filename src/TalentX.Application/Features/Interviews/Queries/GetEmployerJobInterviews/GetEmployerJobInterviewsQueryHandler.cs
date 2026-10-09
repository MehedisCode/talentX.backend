using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;

namespace TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;

public sealed class GetEmployerJobInterviewsQueryHandler
    : IRequestHandler<GetEmployerJobInterviewsQuery, PaginatedList<EmployerInterviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployerJobInterviewsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<EmployerInterviewDto>> Handle(
        GetEmployerJobInterviewsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be logged in to view job interview invitations.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job with ID '{request.JobId}' was not found.");

        if (job.EmployerId != userId)
        {
            throw new ForbiddenException("You cannot view interview invitations for jobs you do not own.");
        }

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var filter = new InterviewInvitationListFilter(request.Status, request.Type);
        var invitations = await _unitOfWork.InterviewInvitations.GetByJobIdPaginatedAsync(
            request.JobId, filter, page, pageSize, cancellationToken);

        // Show the applicant's CURRENT profile name (mirrors the applicants list); the frozen
        // JobApplication.ApplicantName is only a fallback for talents whose profile row is missing.
        var talentUserIds = invitations.Items
            .Where(invite => !string.IsNullOrWhiteSpace(invite.TalentUserId))
            .Select(invite => invite.TalentUserId)
            .Distinct()
            .ToList();
        var talentsByUserId = new Dictionary<string, Talent>();
        foreach (var talent in await _unitOfWork.Talents.GetByUserIdsAsync(talentUserIds, cancellationToken))
        {
            talentsByUserId[talent.UserId] = talent;
        }

        var result = new List<EmployerInterviewDto>();

        foreach (var invite in invitations.Items)
        {
            var timeslotDtos = invite.Timeslots.Select(t => new InterviewTimeslotDto(
                t.Id,
                t.StartTime,
                t.EndTime,
                t.IsBooked,
                t.MeetingUrl)).ToList();

            var talent = talentsByUserId.GetValueOrDefault(invite.TalentUserId);

            result.Add(new EmployerInterviewDto(
                invite.Id,
                invite.JobId,
                invite.JobApplicationId,
                invite.TalentUserId,
                talent?.Name ?? invite.JobApplication?.ApplicantName ?? "Candidate",
                invite.JobApplication?.ApplicantEmail ?? "",
                invite.Type.ToString(),
                invite.Status.ToString(),
                invite.AssessmentUrl,
                invite.AssessmentDeadline,
                invite.LocationOrLink,
                invite.Instructions,
                invite.DeclineReason,
                invite.SelectedTimeslotId,
                invite.ScheduledAt,
                invite.SentAt,
                invite.RespondedAt,
                timeslotDtos));
        }

        return new PaginatedList<EmployerInterviewDto>(result, invitations.Page, invitations.PageSize, invitations.TotalCount);
    }
}
