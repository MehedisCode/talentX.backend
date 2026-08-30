using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;

public record GetMyApplicationStatusQuery(Guid JobId) : IRequest<MyApplicationStatusDto>;
