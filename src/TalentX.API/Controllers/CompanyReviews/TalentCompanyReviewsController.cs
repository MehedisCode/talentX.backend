using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;
using TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetMyCompanyReviews;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.CompanyReviews;

[Route("api/v1/talents/me/company-reviews")]
public sealed class TalentCompanyReviewsController : BaseApiController
{
    /// <summary>
    /// Lists the signed-in talent's applications that have an accepted interview, each with
    /// its review (if any) and whether a review can be written, edited or withdrawn.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of review items, newest unlock first.</returns>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(PaginatedList<MyCompanyReviewItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyCompanyReviews(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyCompanyReviewsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Submits a review of the company for an application whose accepted interview has ended.
    /// The review starts as Pending until an admin approves it.
    /// </summary>
    /// <param name="command">The application, required sentiment, optional feedback and ratings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The id of the new review.</returns>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Guid>> SubmitCompanyReview(
        [FromBody] SubmitCompanyReviewCommand command,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await Sender.Send(command, cancellationToken));

    /// <summary>
    /// Edits the talent's own review. An edited review returns to Pending and is hidden from
    /// public view until an admin approves it again.
    /// </summary>
    /// <param name="reviewId">The review to edit.</param>
    /// <param name="command">The new sentiment, feedback and ratings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPut("{reviewId:guid}")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompanyReview(
        Guid reviewId,
        [FromBody] UpdateCompanyReviewCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { ReviewId = reviewId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Withdraws the talent's own review. This is final: the application can't be reviewed again.
    /// </summary>
    /// <param name="reviewId">The review to withdraw.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("{reviewId:guid}/withdraw")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawCompanyReview(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new WithdrawCompanyReviewCommand(reviewId), cancellationToken);
        return Ok(result);
    }
}
