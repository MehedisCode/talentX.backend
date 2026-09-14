using MediatR;

namespace TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;

public sealed record ToggleJobAlertCommand(Guid Id, bool IsEnabled) : IRequest;
