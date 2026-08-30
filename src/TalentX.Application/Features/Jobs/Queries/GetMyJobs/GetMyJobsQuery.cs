using MediatR;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetMyJobs;

public record GetMyJobsQuery() : IRequest<List<MyJobDto>>;
