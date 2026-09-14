using MediatR;
using TalentX.Application.Features.JobAlerts.DTOs;

namespace TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;

public sealed record UpdateJobAlertCommand(Guid Id, UpdateJobAlertParameters Parameters) : IRequest;
