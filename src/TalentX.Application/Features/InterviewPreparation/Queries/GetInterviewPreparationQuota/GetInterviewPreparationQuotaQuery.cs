using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationQuota;

public sealed record GetInterviewPreparationQuotaQuery
    : IRequest<InterviewPreparationQuotaDto>;
