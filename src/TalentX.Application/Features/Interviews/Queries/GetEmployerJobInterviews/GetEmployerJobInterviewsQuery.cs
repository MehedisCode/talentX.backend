using MediatR;
using TalentX.Application.Features.Interviews.DTOs;

namespace TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;

public sealed record GetEmployerJobInterviewsQuery(Guid JobId) : IRequest<IReadOnlyList<EmployerInterviewDto>>;
