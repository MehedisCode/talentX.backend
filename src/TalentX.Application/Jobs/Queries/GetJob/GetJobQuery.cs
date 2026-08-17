using MediatR;

namespace TalentX.Application.Jobs.Queries.GetJob;

public record GetJobQuery(Guid Id) : IRequest<JobDto?>;
