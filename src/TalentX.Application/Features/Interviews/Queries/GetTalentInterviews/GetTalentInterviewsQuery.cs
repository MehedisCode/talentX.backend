using MediatR;
using TalentX.Application.Features.Interviews.DTOs;

namespace TalentX.Application.Features.Interviews.Queries.GetTalentInterviews;

public sealed record GetTalentInterviewsQuery() : IRequest<IReadOnlyList<TalentInterviewDto>>;
