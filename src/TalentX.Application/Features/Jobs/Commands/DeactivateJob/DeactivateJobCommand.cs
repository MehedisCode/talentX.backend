using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.DeactivateJob;

public sealed record DeactivateJobCommand(
    Guid JobId,
    DeactivationReason Reason,
    string Details
) : IRequest<StatusUpdateResponseDto>;
