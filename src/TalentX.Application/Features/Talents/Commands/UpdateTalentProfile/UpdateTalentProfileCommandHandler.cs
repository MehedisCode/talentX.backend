using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;

public sealed class UpdateTalentProfileCommandHandler : IRequestHandler<UpdateTalentProfileCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTalentProfileCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateTalentProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update your profile.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        talent.UpdateProfile(
            request.Name,
            request.Tagline ?? talent.Tagline,
            request.ShortBio ?? talent.ShortBio,
            request.Location ?? talent.Location,
            request.Phone ?? talent.Phone,
            request.LinkedInUrl ?? talent.LinkedInUrl,
            request.AdditionalUrls ?? talent.AdditionalUrls,
            request.TotalYearsExperience ?? talent.TotalYearsExperience,
            request.ExpectedSalary ?? talent.ExpectedSalary,
            request.SkillPropositions ?? talent.SkillPropositions
        );

        if (request.TechnicalSkills is not null || request.SoftSkills is not null || request.ToolsAndPlatforms is not null)
        {
            talent.UpdateSkills(
                request.TechnicalSkills ?? talent.TechnicalSkills,
                request.SoftSkills ?? talent.SoftSkills,
                request.ToolsAndPlatforms ?? talent.ToolsAndPlatforms
            );
        }

        _unitOfWork.Talents.Update(talent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
