using MediatR;

namespace TalentX.Application.Features.Talents.Queries.GetTalentProfileForEmployer;

public sealed record GetTalentProfileForEmployerQuery(Guid ApplicationId) : IRequest<TalentPublicProfileResponse>;
