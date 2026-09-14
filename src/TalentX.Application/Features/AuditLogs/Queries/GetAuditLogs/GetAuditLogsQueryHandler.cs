using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.AuditLogs.DTOs;

namespace TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var (items, totalCount) = await _unitOfWork.AuditLogs.GetPagedAuditLogsAsync(
            request.SearchQuery,
            request.StartDate,
            request.EndDate,
            page,
            pageSize,
            cancellationToken);

        var auditLogDtos = items.Select(auditLog => new AuditLogDto
        {
            Id = auditLog.Id,
            Timestamp = auditLog.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Actor = auditLog.Actor,
            IsSystem = auditLog.IsSystem,
            Action = auditLog.Action
        }).ToList();

        return new PaginatedList<AuditLogDto>(auditLogDtos, page, pageSize, totalCount);
    }
}
