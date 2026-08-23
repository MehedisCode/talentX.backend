using MediatR;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetJobDetails;

public record GetJobDetailsQuery(Guid Id) : IRequest<JobDetailsDto>;
