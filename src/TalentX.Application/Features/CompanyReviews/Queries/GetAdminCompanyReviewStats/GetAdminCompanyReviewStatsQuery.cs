using MediatR;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;

public sealed record GetAdminCompanyReviewStatsQuery : IRequest<AdminCompanyReviewStatsDto>;
