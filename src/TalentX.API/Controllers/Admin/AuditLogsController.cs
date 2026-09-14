using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.AuditLogs.Queries.GetAuditLogs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[Authorize(Roles = nameof(UserRole.Admin))]
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
