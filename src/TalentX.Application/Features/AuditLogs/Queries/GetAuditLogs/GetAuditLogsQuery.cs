using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AuditLogs.DTOs;

namespace TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? SearchQuery = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<AuditLogDto>>;
