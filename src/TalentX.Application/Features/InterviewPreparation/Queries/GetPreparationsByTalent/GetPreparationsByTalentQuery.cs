using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public sealed record GetPreparationsByTalentQuery : IRequest<IReadOnlyList<InterviewPreparationDto>>;
