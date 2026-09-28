using MediatR;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;

public sealed record GetAdminCompanyReviewDetailsQuery(Guid ReviewId) : IRequest<AdminCompanyReviewDetailsDto>;
