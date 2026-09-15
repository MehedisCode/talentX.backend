using MediatR;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Commands.UpdateCompanyProfile;

public sealed record UpdateCompanyProfileCommand(
    string CompanyName,
    string? Description,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Location,
    string? LinkedIn,
    string? Twitter,
    string? Facebook
) : IRequest<CompanyProfileDto>;
