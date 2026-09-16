using TalentX.Application.Common.Models;
using TalentX.Application.Features.AdminEmployerVerification.DTOs;
using TalentX.Domain.Enums;
using MediatR;

namespace TalentX.Application.Features.AdminEmployerVerification.Queries.GetPendingEmployers;

public sealed record GetPendingEmployersQuery : IRequest<PaginatedList<PendingEmployerDto>>
{
    public string? SearchTerm { get; init; }
    public EmployerVerificationStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
