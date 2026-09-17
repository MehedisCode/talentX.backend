using MediatR;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetAdminJobDetails;

public sealed record GetAdminJobDetailsQuery(Guid JobId) : IRequest<AdminJobDetailsDto>;
