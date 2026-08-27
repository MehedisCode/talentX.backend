using MediatR;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfile;

public sealed record GetTalentProfileQuery(string UserId) : IRequest<TalentProfileResponse>;
