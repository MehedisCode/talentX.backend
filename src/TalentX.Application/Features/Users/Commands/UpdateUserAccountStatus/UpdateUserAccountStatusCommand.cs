using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;

public sealed record UpdateUserAccountStatusCommand(
    Guid UserId,
    AccountStatus NewStatus
) : IRequest<StatusUpdateResponseDto>;
