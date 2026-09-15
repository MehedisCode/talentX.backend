using MediatR;
using TalentX.Application.Features.JobAlerts.DTOs;

namespace TalentX.Application.Features.JobAlerts.Queries.GetJobAlerts;

public sealed record GetJobAlertsQuery : IRequest<List<JobAlertDto>>;
