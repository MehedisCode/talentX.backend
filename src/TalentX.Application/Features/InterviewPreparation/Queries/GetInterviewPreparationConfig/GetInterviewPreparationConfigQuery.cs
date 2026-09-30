using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationConfig;

public sealed record GetInterviewPreparationConfigQuery : IRequest<InterviewPreparationConfigDto>;
