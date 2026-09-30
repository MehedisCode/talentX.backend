using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetPublicCompanyReviews;

public sealed class GetPublicCompanyReviewsQueryHandler
    : IRequestHandler<GetPublicCompanyReviewsQuery, PaginatedList<PublicCompanyReviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicCompanyReviewsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<PublicCompanyReviewDto>> Handle(
        GetPublicCompanyReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            company = await _unitOfWork.Companies.GetByEmployerIdAsync(request.CompanyId.ToString(), cancellationToken)
                ?? throw new NotFoundException("Company was not found.");
        }

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var reviews = await _unitOfWork.CompanyReviews.GetPublishedByCompanyIdPagedAsync(
            company.Id, page, pageSize, cancellationToken);

        var items = reviews.Items.Select(ToDto).ToList();

        return new PaginatedList<PublicCompanyReviewDto>(items, reviews.Page, reviews.PageSize, reviews.TotalCount);
    }

    private static PublicCompanyReviewDto ToDto(CompanyReview review) => new(
        review.Id,
        review.Sentiment,
        review.Comment,
        review.RecruitmentProcessRating,
        review.CommunicationRating,
        review.InterviewExperienceRating,
        review.ProfessionalismRating,
        review.PublishedAt!.Value);
}
