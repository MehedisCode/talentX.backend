namespace TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;

using MediatR;
using TalentX.Application.Features.AiConfiguration.DTOs;

public sealed record UpdateAiConfigurationCommand(string ModelName, string? ApiKey) : IRequest<AiConfigurationDto>;
