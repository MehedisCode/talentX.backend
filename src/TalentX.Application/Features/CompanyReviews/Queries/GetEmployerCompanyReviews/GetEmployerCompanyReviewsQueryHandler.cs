using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;

public sealed class GetEmployerCompanyReviewsQueryHandler
    : IRequestHandler<GetEmployerCompanyReviewsQuery, PaginatedList<EmployerCompanyReviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployerCompanyReviewsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<EmployerCompanyReviewDto>> Handle(
        GetEmployerCompanyReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId!;

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(employerId, cancellationToken);
        if (company is null)
        {
            return new PaginatedList<EmployerCompanyReviewDto>(Array.Empty<EmployerCompanyReviewDto>(), page, pageSize, 0);
        }

        var reviews = await _unitOfWork.CompanyReviews.GetByCompanyIdPagedAsync(
            company.Id, request.Status, request.Sentiment, page, pageSize, cancellationToken);

        var items = reviews.Items.Select(ToDto).ToList();

        return new PaginatedList<EmployerCompanyReviewDto>(items, reviews.Page, reviews.PageSize, reviews.TotalCount);
    }

    private static EmployerCompanyReviewDto ToDto(CompanyReview review) => new(
        review.Id,
        review.Status,
        review.Sentiment,
        review.Comment,
        new CompanyReviewRatingsDto(
            review.RecruitmentProcessRating,
            review.CommunicationRating,
            review.InterviewExperienceRating,
            review.ProfessionalismRating),
        review.JobTitle,
        review.SubmittedAt,
        review.UpdatedAt);
}
