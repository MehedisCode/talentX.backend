using MediatR;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetPublicCompanyProfile;

public sealed record GetPublicCompanyProfileQuery(Guid Id) : IRequest<PublicCompanyProfileDto>;
