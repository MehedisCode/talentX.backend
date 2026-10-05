namespace TalentX.Application.Features.AiConfiguration.DTOs;

public sealed record AiConfigurationDto(
    string ModelName,
    string MaskedApiKey,
    bool HasCustomKey,
    DateTime? UpdatedAt);
