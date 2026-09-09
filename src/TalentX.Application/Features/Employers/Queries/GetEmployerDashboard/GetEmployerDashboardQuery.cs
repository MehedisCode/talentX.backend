using MediatR;
using TalentX.Application.Features.Employers.DTOs;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;

public sealed record GetEmployerDashboardQuery : IRequest<EmployerDashboardDto>;
