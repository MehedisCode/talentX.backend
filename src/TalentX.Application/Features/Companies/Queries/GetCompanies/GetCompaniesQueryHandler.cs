using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetCompanies;

public sealed class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, CompanyListResult>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private const int DefaultPageSize = 10;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresignedUrlService _presignedUrlService;

    public GetCompaniesQueryHandler(
        IUnitOfWork unitOfWork,
        IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<CompanyListResult> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > PaginationRequest.MaxPageSize
            ? DefaultPageSize
            : request.PageSize;
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "name" : request.SortBy.Trim().ToLowerInvariant();
        var sortDir = string.IsNullOrWhiteSpace(request.SortDir) ? "asc" : request.SortDir.Trim().ToLowerInvariant();

        var paged = await _unitOfWork.Companies.GetAllAsync(
            request.Keyword,
            request.Industry,
            request.Location,
            request.CompanySize,
            sortBy,
            sortDir,
            page,
            pageSize,
            cancellationToken,
            request.Hiring);

        var items = new List<CompanyListItemDto>(paged.Items.Count);
        foreach (var item in paged.Items)
        {
            string? logoUrl = null;
            if (!string.IsNullOrWhiteSpace(item.LogoStorageKey))
            {
                logoUrl = await _presignedUrlService.CreateDownloadUrlAsync(item.LogoStorageKey, LogoUrlExpiry, cancellationToken);
            }

            items.Add(new CompanyListItemDto(
                item.Id,
                item.Name,
                logoUrl,
                item.Description,
                item.Industry,
                item.CompanySize,
                item.Website,
                item.Location,
                item.LinkedInUrl,
                item.TwitterUrl,
                item.FacebookUrl,
                item.OpeningsCount,
                item.VacanciesCount));
        }

        return new CompanyListResult(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages, paged.TotalOpenings, paged.TotalVacancies);
    }
}
