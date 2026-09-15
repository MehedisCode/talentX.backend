using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Jobs.Commands.GenerateJobRequirements;

/// <summary>
/// Handles the GenerateJobRequirementsCommand by calling the AI service.
/// </summary>
public sealed class GenerateJobRequirementsCommandHandler
    : IRequestHandler<GenerateJobRequirementsCommand, GenerateJobRequirementsResponse>
{
    private readonly IJobGenerationService _generationService;

    public GenerateJobRequirementsCommandHandler(IJobGenerationService generationService)
    {
        _generationService = generationService;
    }

    public async Task<GenerateJobRequirementsResponse> Handle(
        GenerateJobRequirementsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _generationService.GenerateRequirementsAsync(
            request.Title,
            request.CompanyName,
            request.EmploymentType,
            request.ExperienceLevel,
            request.WorkArrangement,
            request.RequiredSkills,
            cancellationToken);

        return new GenerateJobRequirementsResponse(result.Requirements);
    }
}
