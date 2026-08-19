using MediatR;

namespace TalentX.Application.Features.Jobs.Queries.GetJob;

public record GetJobQuery(Guid Id) : IRequest<JobDto?>;
