using MediatR;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;

public sealed record GetTalentApplicationPrefillQuery : IRequest<TalentApplicationPrefillResponse>;
