using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Jobs.Commands.GenerateSkills;

/// <summary>
/// Handles the GenerateSkillsCommand by calling the AI service.
/// </summary>
public sealed class GenerateSkillsCommandHandler
    : IRequestHandler<GenerateSkillsCommand, GenerateSkillsResponse>
{
    private readonly IJobGenerationService _generationService;

    public GenerateSkillsCommandHandler(IJobGenerationService generationService)
    {
        _generationService = generationService;
    }

    public async Task<GenerateSkillsResponse> Handle(
        GenerateSkillsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _generationService.GenerateSkillsAsync(
            request.Title,
            request.CompanyName,
            request.EmploymentType,
            request.ExperienceLevel,
            request.WorkArrangement,
            cancellationToken);

        return new GenerateSkillsResponse(result.Skills);
    }
}
