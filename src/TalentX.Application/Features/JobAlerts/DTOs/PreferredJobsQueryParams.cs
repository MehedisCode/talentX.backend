using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.JobAlerts.DTOs;

public sealed record PreferredJobsQueryParams(
    Guid? AlertId = null,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize);
