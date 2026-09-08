using MediatR;

namespace TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;

public sealed record RunJobTalentMatchingCommand(Guid JobId) : IRequest<RunJobTalentMatchingResponse>;
