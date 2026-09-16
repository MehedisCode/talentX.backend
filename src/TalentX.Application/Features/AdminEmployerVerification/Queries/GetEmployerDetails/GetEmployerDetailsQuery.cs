using MediatR;
using TalentX.Application.Features.AdminEmployerVerification.DTOs;

namespace TalentX.Application.Features.AdminEmployerVerification.Queries.GetEmployerDetails;

public sealed record GetEmployerDetailsQuery : IRequest<EmployerDetailDto?>
{
    public Guid EmployerId { get; init; }
}
