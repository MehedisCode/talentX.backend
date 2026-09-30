using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;

public sealed class GetEmployerCompanyReviewSummaryQueryHandler
    : IRequestHandler<GetEmployerCompanyReviewSummaryQuery, EmployerCompanyReviewSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployerCompanyReviewSummaryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<EmployerCompanyReviewSummaryDto> Handle(
        GetEmployerCompanyReviewSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId!;

        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(employerId, cancellationToken);
        if (company is null)
        {
            return new EmployerCompanyReviewSummaryDto(
                CompanyReviewSummaryCalculator.Empty,
                ToCountsByStatus(new Dictionary<CompanyReviewStatus, int>()));
        }

        var summary = await _unitOfWork.CompanyReviews.GetPublishedSummaryAsync(company.Id, cancellationToken);
        var counts = await _unitOfWork.CompanyReviews.GetStatusCountsByCompanyAsync(company.Id, cancellationToken);

        return new EmployerCompanyReviewSummaryDto(summary, ToCountsByStatus(counts));
    }

    private static IReadOnlyDictionary<string, int> ToCountsByStatus(IReadOnlyDictionary<CompanyReviewStatus, int> counts) =>
        Enum.GetValues<CompanyReviewStatus>()
            .ToDictionary(status => status.ToString(), status => counts.GetValueOrDefault(status));
}
