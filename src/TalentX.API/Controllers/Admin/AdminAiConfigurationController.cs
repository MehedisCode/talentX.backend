using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;
using TalentX.Application.Features.AiConfiguration.DTOs;
using TalentX.Application.Features.AiConfiguration.Queries.GetAiConfiguration;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/ai-configuration")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminAiConfigurationController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAiConfigurationQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] UpdateAiConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateAiConfigurationCommand(request.ModelName, request.ApiKey),
            cancellationToken);
        return Ok(result);
    }
}
