using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;
using TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobApplications;

[ApiController]
[Route("api/v1/job-applications")]
[Authorize(Roles = $"{nameof(UserRole.Employer)},{nameof(UserRole.Talent)}")]
public sealed class JobApplicationDetailsController : BaseApiController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJobApplicationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Employer))]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateApplicationStatusCommand(id, request.NewStatus), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/candidate-profile")]
    [Authorize(Roles = nameof(UserRole.Employer))]
    public async Task<IActionResult> GetCandidateProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTalentProfileForEmployerQuery(id), cancellationToken);
        return Ok(result);
    }
}
