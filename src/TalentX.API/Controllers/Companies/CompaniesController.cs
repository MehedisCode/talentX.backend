using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Companies.Queries.GetCompanyPublishedJobs;
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
    /// Gets public company profile details and active vacancies count by company ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await Sender.Send(new GetPublicCompanyProfileQuery(id), cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Gets active, published vacancies for a company by company ID.
    /// </summary>
    [HttpGet("{id:guid}/jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyPublishedJobs(Guid id, CancellationToken cancellationToken)
    {
        var jobs = await Sender.Send(new GetCompanyPublishedJobsQuery(id), cancellationToken);
        return Ok(jobs);
    }
}
