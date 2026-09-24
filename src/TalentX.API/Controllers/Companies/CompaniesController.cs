using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Companies.DTOs;
using TalentX.Application.Features.Companies.Queries.GetCompanies;
using TalentX.Application.Features.Companies.Queries.GetPublicCompanyProfile;

namespace TalentX.API.Controllers.Companies;

/// <summary>
/// Public endpoints for browsing company profiles and open published vacancies.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class CompaniesController : BaseApiController
{
    /// <summary>
    /// Gets a paginated public company list with available vacancy aggregates.
    /// Defaults to 10 companies per page sorted by name A-Z.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanies(
        [FromQuery] CompanyQueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = queryParams ?? new CompanyQueryParams();
        var result = await Sender.Send(new GetCompaniesQuery(parameters), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets public company profile details and active vacancies count by company ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await Sender.Send(new GetPublicCompanyProfileQuery(id), cancellationToken);
        return Ok(profile);
    }
}
