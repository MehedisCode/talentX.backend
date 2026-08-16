using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;

namespace TalentX.Api.Controllers;

/// <summary>
/// Handles endpoints for employer account operations and management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public sealed class EmployersController : ControllerBase
{
    private readonly ISender _sender;

    public EmployersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers and creates a new employer profile.
    /// </summary>
    /// <param name="command">The employer registration command details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created employer data transfer object.</returns>
    /// <response code="201">Employer successfully created.</response>
    /// <response code="400">Request validation failed.</response>
    /// <response code="409">Employer with given email already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
