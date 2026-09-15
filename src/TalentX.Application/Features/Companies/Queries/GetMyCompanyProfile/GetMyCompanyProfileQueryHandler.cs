using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Companies.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;

namespace TalentX.Application.Features.Companies.Queries.GetMyCompanyProfile;

public sealed class GetMyCompanyProfileQueryHandler : IRequestHandler<GetMyCompanyProfileQuery, CompanyProfileDto>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPresignedUrlService _presignedUrlService;

    public GetMyCompanyProfileQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<CompanyProfileDto> Handle(GetMyCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated as an employer to view your company profile.");

        var employer = await _unitOfWork.Employers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Employer profile for user '{userId}' was not found.");

        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(userId, cancellationToken);

        if (company is null)
        {
            var companyName = !string.IsNullOrWhiteSpace(employer.CompanyName)
                ? employer.CompanyName
                : employer.Name;

            company = new Company(userId, companyName);
            await _unitOfWork.Companies.AddAsync(company, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        string? logoUrl = null;
        if (!string.IsNullOrWhiteSpace(company.LogoStorageKey))
        {
            logoUrl = await _presignedUrlService.CreateDownloadUrlAsync(company.LogoStorageKey, LogoUrlExpiry, cancellationToken);
        }

        return new CompanyProfileDto(
            company.Id,
            company.EmployerId,
            company.Name,
            logoUrl,
            company.Description,
            company.Industry,
            company.CompanySize,
            company.Website,
            company.Location,
            company.LinkedInUrl,
            company.TwitterUrl,
            company.FacebookUrl
        );
    }
}
