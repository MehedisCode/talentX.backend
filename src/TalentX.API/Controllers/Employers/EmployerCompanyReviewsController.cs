using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Employers;

/// <summary>
/// An employer's read-only, anonymized view of the reviews written about their company. There
/// are no write actions here: employers can see a review's status, sentiment, feedback and
/// ratings, but never who wrote it, and can't edit, delete, or otherwise act on it.
/// </summary>
[ApiController]
[Route("api/v1/employers/me/company-reviews")]
[Authorize(Roles = nameof(UserRole.Employer))]
public sealed class EmployerCompanyReviewsController : BaseApiController
{
    /// <summary>
    /// Gets a paginated, filterable list of every review written about the signed-in employer's
    /// company, in any status (Pending, Published, Rejected, Hidden, Withdrawn).
    /// </summary>
    /// <param name="query">Status/sentiment filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of anonymized review items.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<EmployerCompanyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompanyReviews(
        [FromQuery] GetEmployerCompanyReviewsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetEmployerCompanyReviewsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the signed-in employer's company review summary (Published reviews only, matching
    /// what the public sees) plus a count of reviews per status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The published-reviews summary and per-status counts.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(EmployerCompanyReviewSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompanyReviewSummary(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEmployerCompanyReviewSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
