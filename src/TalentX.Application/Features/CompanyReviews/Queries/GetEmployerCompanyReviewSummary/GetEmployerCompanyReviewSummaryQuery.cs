using MediatR;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;

public sealed record GetEmployerCompanyReviewSummaryQuery : IRequest<EmployerCompanyReviewSummaryDto>;
