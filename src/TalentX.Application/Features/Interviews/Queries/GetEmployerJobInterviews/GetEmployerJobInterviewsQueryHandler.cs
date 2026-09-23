using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.DTOs;
using TalentX.Application.Identity.Services;

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

        var result = new List<EmployerInterviewDto>();

        foreach (var invite in invitations.Items)
        {
            var timeslotDtos = invite.Timeslots.Select(t => new InterviewTimeslotDto(
                t.Id,
                t.StartTime,
                t.EndTime,
                t.IsBooked,
                t.MeetingUrl)).ToList();

            result.Add(new EmployerInterviewDto(
                invite.Id,
                invite.JobId,
                invite.JobApplicationId,
                invite.TalentUserId,
                invite.JobApplication?.ApplicantName ?? "Candidate",
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
