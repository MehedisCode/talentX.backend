namespace TalentX.Application.Features.AiConfiguration.Queries.GetAiConfiguration;

using MediatR;
using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.AiConfiguration.Constants;
using TalentX.Application.Features.AiConfiguration.DTOs;

public sealed class GetAiConfigurationQueryHandler : IRequestHandler<GetAiConfigurationQuery, AiConfigurationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public GetAiConfigurationQueryHandler(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<AiConfigurationDto> Handle(GetAiConfigurationQuery request, CancellationToken cancellationToken)
    {
        var existingConfiguration = await _unitOfWork.AiConfigurations.GetAsync(cancellationToken);

        if (existingConfiguration is null)
        {
            var fallbackModelName = _configuration["GEMINI_MODEL_NAME"]
                ?? _configuration["Gemini:ModelName"]
                ?? AiConfigurationConstants.DefaultModelName;

            var environmentApiKey = _configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"];
            var hasEnvironmentKey = !string.IsNullOrEmpty(environmentApiKey);
            var maskedApiKey = hasEnvironmentKey
                ? AiConfigurationConstants.EnvironmentKeyMasked
                : AiConfigurationConstants.NotConfigured;

            return new AiConfigurationDto(fallbackModelName, maskedApiKey, hasEnvironmentKey, null);
        }

        var hasCustomKey = !string.IsNullOrWhiteSpace(existingConfiguration.EncryptedApiKey);
        var maskedCustomKey = hasCustomKey
            ? AiConfigurationConstants.MaskedKeyPlaceholder
            : AiConfigurationConstants.NotConfigured;

        return new AiConfigurationDto(
            existingConfiguration.ModelName,
            maskedCustomKey,
            hasCustomKey,
            existingConfiguration.UpdatedAt ?? existingConfiguration.CreatedAt);
    }
}
