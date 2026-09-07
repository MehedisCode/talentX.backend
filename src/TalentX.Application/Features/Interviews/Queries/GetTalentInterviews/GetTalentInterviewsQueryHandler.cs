using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Interviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Queries.GetTalentInterviews;

public sealed class GetTalentInterviewsQueryHandler
    : IRequestHandler<GetTalentInterviewsQuery, IReadOnlyList<TalentInterviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTalentInterviewsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<TalentInterviewDto>> Handle(
        GetTalentInterviewsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be logged in to view your interview invitations.");

        var invitations = await _unitOfWork.InterviewInvitations.GetByTalentUserIdAsync(userId, cancellationToken);
        var result = new List<TalentInterviewDto>();

        foreach (var invite in invitations)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(invite.JobId, cancellationToken);

            // Meeting URL stays hidden until the candidate has accepted a specific timeslot,
            // so it can't be scraped from a pending/declined invitation.
            string? revealedLocationOrLink = null;
            if (invite.Status == InterviewInvitationStatus.Accepted)
            {
                var bookedSlot = invite.Timeslots.FirstOrDefault(t => t.Id == invite.SelectedTimeslotId);
                revealedLocationOrLink = bookedSlot?.MeetingUrl ?? invite.LocationOrLink;
            }

            var timeslotDtos = invite.Timeslots.Select(t => new InterviewTimeslotDto(
                t.Id,
                t.StartTime,
                t.EndTime,
                t.IsBooked,
                null // per-slot meeting URLs stay hidden in the list; only the booked slot is revealed above
            )).ToList();

            result.Add(new TalentInterviewDto(
                invite.Id,
                invite.JobId,
                invite.JobApplicationId,
                job?.Title ?? "Job Position",
                job?.CompanyName ?? "Company",
                invite.Type.ToString(),
                invite.Status.ToString(),
                invite.Status == InterviewInvitationStatus.Accepted ? invite.AssessmentUrl : null,
                invite.AssessmentDeadline,
                revealedLocationOrLink,
                invite.Instructions,
                invite.SelectedTimeslotId,
                invite.ScheduledAt,
                invite.SentAt,
                invite.RespondedAt,
                timeslotDtos));
        }

        return result;
    }
}
