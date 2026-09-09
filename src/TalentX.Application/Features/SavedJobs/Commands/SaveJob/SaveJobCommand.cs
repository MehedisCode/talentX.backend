using MediatR;

namespace TalentX.Application.Features.SavedJobs.Commands.SaveJob;

public record SaveJobCommand(Guid JobId) : IRequest<Guid>;
