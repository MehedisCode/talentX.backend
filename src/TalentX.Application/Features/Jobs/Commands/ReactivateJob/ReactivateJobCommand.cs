using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.Jobs.Commands.ReactivateJob;

public sealed record ReactivateJobCommand(Guid JobId) : IRequest<StatusUpdateResponseDto>;
