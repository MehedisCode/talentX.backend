using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;

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
}
