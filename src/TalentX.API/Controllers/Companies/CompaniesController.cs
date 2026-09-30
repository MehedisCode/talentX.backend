using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetPublicCompanyReviews;
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
    /// Gets public company profile details, active vacancies count, and the published review
    /// summary (sentiment split and per-area averages) by company ID.
    /// </summary>
    /// <param name="id">The company id, or the owning employer's user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public company profile.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicCompanyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await Sender.Send(new GetPublicCompanyProfileQuery(id), cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Gets a paginated list of a company's published (admin-approved) reviews. Contains no
    /// reviewer, job, application, or moderation information.
    /// </summary>
    /// <param name="id">The company id, or the owning employer's user id.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of published reviews, newest first.</returns>
    [HttpGet("{id:guid}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedList<PublicCompanyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyReviews(
        Guid id,
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPublicCompanyReviewsQuery(id, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
