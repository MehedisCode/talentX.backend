using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Queries.GetApplicationStatusHistory;

public sealed record GetApplicationStatusHistoryQuery(
    Guid ApplicationId) : IRequest<IReadOnlyList<ApplicationStatusHistoryDto>>;
