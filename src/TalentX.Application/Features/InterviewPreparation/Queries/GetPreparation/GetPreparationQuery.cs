using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;

public sealed record GetPreparationQuery(Guid InvitationId) : IRequest<InterviewPreparationDto>;
