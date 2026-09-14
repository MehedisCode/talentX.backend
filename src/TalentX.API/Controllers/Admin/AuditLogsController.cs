using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;

namespace TalentX.API.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/audit-logs")]
[Route("api/v1/admin/audit-logs")]
public class AuditLogsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] GetAuditLogsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetAuditLogsQuery(), cancellationToken);
        return Ok(result);
    }
}
