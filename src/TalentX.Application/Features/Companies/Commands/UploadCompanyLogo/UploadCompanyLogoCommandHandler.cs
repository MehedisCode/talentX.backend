using System.IO;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;
using TalentX.Domain;

namespace TalentX.Application.Features.Companies.Commands.UploadCompanyLogo;

public sealed class UploadCompanyLogoCommandHandler : IRequestHandler<UploadCompanyLogoCommand, UploadCompanyLogoResponse>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ILogger<UploadCompanyLogoCommandHandler> _logger;

    public UploadCompanyLogoCommandHandler(
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPresignedUrlService presignedUrlService,
        ILogger<UploadCompanyLogoCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _presignedUrlService = presignedUrlService;
        _logger = logger;
    }

    public async Task<UploadCompanyLogoResponse> Handle(UploadCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated as an employer to upload a company logo.");

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
        }

        var sanitizedFileName = TalentMediaStorageKeyGuard.SanitizeFileNameSegment(request.File.FileName);
        var extension = Path.GetExtension(sanitizedFileName);
        var storageKey = $"company-logos/{userId}/{Guid.NewGuid()}{extension}";

        if (!string.IsNullOrWhiteSpace(company.LogoStorageKey))
        {
            try
            {
                await _fileStorageService.DeleteAsync(company.LogoStorageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete previous company logo {StorageKey}", company.LogoStorageKey);
            }
        }

        try
        {
            await _fileStorageService.SaveAsync(request.File, storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save company logo {FileName} to storage", sanitizedFileName);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }

        company.UpdateLogo(storageKey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var logoUrl = await _presignedUrlService.CreateDownloadUrlAsync(storageKey, LogoUrlExpiry, cancellationToken);

        return new UploadCompanyLogoResponse(storageKey, logoUrl);
    }
}
