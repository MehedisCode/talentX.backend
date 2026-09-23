using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobReports.Commands.DismissJobReport;
using TalentX.Application.Features.JobReports.Commands.ResolveJobReport;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;
using TalentX.Application.Features.JobReports.Queries.GetJobReports;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/job-reports")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class JobReportsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] AdminJobReportQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = new GetJobReportsQuery
        {
            SearchQuery = queryParams.SearchQuery,
            Status = queryParams.Status,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
        var result = await Sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReportDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJobReportDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(
        Guid id,
        [FromBody] ResolveJobReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ResolveJobReportCommand(id, request.DeactivationReason, request.Details, request.AdminNotes);
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/dismiss")]
    public async Task<IActionResult> DismissReport(
        Guid id,
        [FromBody] DismissJobReportRequestDto? request,
        CancellationToken cancellationToken)
    {
        var command = new DismissJobReportCommand(id, request?.AdminNotes);
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
