using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;

public sealed record UpdateApplicationStatusCommand(
    Guid ApplicationId,
    ApplicationStatus NewStatus
) : IRequest<StatusUpdateResponseDto>;
