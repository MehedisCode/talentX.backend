using MediatR;
using TalentX.Application.Features.JobAlerts.DTOs;

namespace TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;

public sealed record CreateJobAlertCommand(CreateJobAlertParameters Parameters) : IRequest<Guid>;
