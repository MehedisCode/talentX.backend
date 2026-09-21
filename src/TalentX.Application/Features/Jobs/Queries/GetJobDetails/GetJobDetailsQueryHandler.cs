using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetJobDetails;

public class GetJobDetailsQueryHandler : IRequestHandler<GetJobDetailsQuery, JobDetailsDto>
{
    private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPresignedUrlService _presignedUrlService;

    public GetJobDetailsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IPresignedUrlService presignedUrlService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _presignedUrlService = presignedUrlService;
    }

    public async Task<JobDetailsDto> Handle(GetJobDetailsQuery request, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetPublishedJobByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.Id}' was not found.");

        var dto = _mapper.Map<JobDetailsDto>(job);
        dto.Status = job.Status == Domain.Enums.JobStatus.Published && job.ExpireAt.Date < DateTime.UtcNow.Date
            ? "Expired"
            : job.Status.ToString();

        // Company entity is the source of truth for name/logo/link.
        // Fall back to the denormalized Job.CompanyName for legacy jobs without a Company profile.
        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(job.EmployerId, cancellationToken);
        if (company is not null)
        {
            dto.CompanyId = company.Id;
            dto.CompanyName = company.Name;
            if (!string.IsNullOrWhiteSpace(company.LogoStorageKey))
            {
                dto.LogoUrl = await _presignedUrlService.CreateDownloadUrlAsync(
                    company.LogoStorageKey, LogoUrlExpiry, cancellationToken);
            }
        }

        return dto;
    }
}
