using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

/// <summary>
/// Admin moderation queue for talent-submitted company reviews.
/// </summary>
// Named explicitly (not "ReviewsController") to avoid a Swagger schema/operation-id clash with
// the employer-facing company reviews controller added in a later part of this feature.
[ApiController]
[Route("api/v1/admin/company-reviews")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminCompanyReviewsController : BaseApiController
{
    /// <summary>
    /// Gets a paginated, filterable list of company reviews for moderation.
    /// </summary>
    /// <param name="query">Search text, status/sentiment filters, and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of review summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AdminCompanyReviewListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompanyReviews(
        [FromQuery] GetAdminCompanyReviewsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetAdminCompanyReviewsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the count of reviews per status, used for the moderation queue's tab badges.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts per review status and the total.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminCompanyReviewStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompanyReviewStats(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminCompanyReviewStatsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets full details for a single company review, including the reviewer's identity and
    /// the last moderation decision.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review's details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminCompanyReviewDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyReviewDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminCompanyReviewDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Approves a pending review, publishing it. Writes an audit log entry.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{id:guid}/approve")]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveCompanyReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ApproveCompanyReviewCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Rejects a pending review with a reason and a user-facing explanation shown to the
    /// talent who wrote it. Writes an audit log entry.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="command">The rejection reason and explanation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{id:guid}/reject")]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectCompanyReview(
        Guid id,
        [FromBody] RejectCompanyReviewCommand command,
        CancellationToken cancellationToken)
    {
        // The route is the source of truth for which review; any ReviewId in the body is ignored.
        var result = await Sender.Send(command with { ReviewId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Hides a published review that violates policy, with a reason and a user-facing
    /// explanation shown to the talent who wrote it. Writes an audit log entry.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="command">The hide reason and explanation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{id:guid}/hide")]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideCompanyReview(
        Guid id,
        [FromBody] HideCompanyReviewCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { ReviewId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Restores a hidden review to Published. Writes an audit log entry.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{id:guid}/unhide")]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnhideCompanyReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UnhideCompanyReviewCommand(id), cancellationToken);
        return Ok(result);
    }
}
