using MediatR;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetMyCompanyProfile;

public sealed record GetMyCompanyProfileQuery : IRequest<CompanyProfileDto>;
