namespace TalentX.Application.Features.AiConfiguration.DTOs;

public sealed record UpdateAiConfigRequest(string ModelName, string? ApiKey);
