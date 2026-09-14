using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AuditLogs.DTOs;

namespace TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery : IRequest<PaginatedList<AuditLogDto>>
{
    public string? SearchQuery { get; init; }
    public string? ActionType { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}
