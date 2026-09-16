using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Users.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.Queries.GetUserDetails;

public sealed class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, AdminUserDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminUserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || user.Role == UserRole.Admin)
        {
            throw new NotFoundException("User", request.UserId);
        }

        AdminTalentDetailsSectionDto? talentDetails = null;
        AdminEmployerDetailsSectionDto? employerDetails = null;

        if (user.Role == UserRole.Talent)
        {
            var talent = await _unitOfWork.Talents.GetByUserIdAsync(user.UserId, cancellationToken);
            if (talent is not null)
            {
                talentDetails = new AdminTalentDetailsSectionDto
                {
                    Tagline = talent.Tagline,
                    ShortBio = talent.ShortBio,
                    Location = talent.Location,
                    Phone = talent.Phone,
                    LinkedInUrl = talent.LinkedInUrl,
                    AdditionalUrls = talent.AdditionalUrls,
                    TotalYearsExperience = talent.TotalYearsExperience,
                    ExpectedSalary = talent.ExpectedSalary,
                    SkillPropositions = talent.SkillPropositions,
                    TechnicalSkills = talent.TechnicalSkills,
                    SoftSkills = talent.SoftSkills,
                    ToolsAndPlatforms = talent.ToolsAndPlatforms
                };
            }
        }
        else if (user.Role == UserRole.Employer)
        {
            var employer = await _unitOfWork.Employers.GetByUserIdAsync(user.UserId, cancellationToken);
            if (employer is not null)
            {
                employerDetails = new AdminEmployerDetailsSectionDto
                {
                    CompanyName = employer.CompanyName,
                    VerificationStatus = employer.VerificationStatus
                };
            }
        }

        return new AdminUserDetailsDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsVerified = user.IsVerified,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            TalentDetails = talentDetails,
            EmployerDetails = employerDetails
        };
    }
}
