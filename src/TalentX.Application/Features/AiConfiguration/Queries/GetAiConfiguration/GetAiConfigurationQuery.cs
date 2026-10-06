namespace TalentX.Application.Features.AiConfiguration.Queries.GetAiConfiguration;

using MediatR;
using TalentX.Application.Features.AiConfiguration.DTOs;

public sealed record GetAiConfigurationQuery : IRequest<AiConfigurationDto>;
