using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.AdminEmployerVerification.Commands.ApproveEmployer;
using TalentX.Application.Features.AdminEmployerVerification.Commands.RejectEmployer;
using TalentX.Application.Features.AdminEmployerVerification.Queries.GetEmployerDetails;
using TalentX.Application.Features.AdminEmployerVerification.Queries.GetPendingEmployers;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/v1/admin/employers")]
public class AdminEmployerVerificationController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetEmployers(
        [FromQuery] GetPendingEmployersQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetPendingEmployersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployerDetails(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetEmployerDetailsQuery { EmployerId = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveEmployer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await Sender.Send(new ApproveEmployerCommand { EmployerId = id }, cancellationToken);
        return Ok(new { message = "Employer account approved successfully." });
    }

    public sealed record RejectEmployerRequestBody(string Reason);

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectEmployer(
        Guid id,
        [FromBody] RejectEmployerRequestBody body,
        CancellationToken cancellationToken = default)
    {
        await Sender.Send(new RejectEmployerCommand { EmployerId = id, Reason = body.Reason }, cancellationToken);
        return Ok(new { message = "Employer account rejected successfully." });
    }
}
