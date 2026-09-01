using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Jobs.Commands.GenerateJobDescription;

/// <summary>
/// Handles the GenerateJobDescriptionCommand by calling the AI service.
/// </summary>
public sealed class GenerateJobDescriptionCommandHandler
    : IRequestHandler<GenerateJobDescriptionCommand, GenerateJobDescriptionResponse>
{
    private readonly IJobGenerationService _generationService;

    public GenerateJobDescriptionCommandHandler(IJobGenerationService generationService)
    {
        _generationService = generationService;
    }

    public async Task<GenerateJobDescriptionResponse> Handle(
        GenerateJobDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _generationService.GenerateDescriptionAsync(
            request.Title,
            request.CompanyName,
            request.EmploymentType,
            request.ExperienceLevel,
            request.WorkArrangement,
            request.RequiredSkills,
            cancellationToken);

        return new GenerateJobDescriptionResponse(result.Description);
    }
}
