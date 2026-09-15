using MediatR;

namespace TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;

public sealed record DeleteJobAlertCommand(Guid Id) : IRequest;
