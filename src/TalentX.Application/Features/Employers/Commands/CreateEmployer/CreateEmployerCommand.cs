using MediatR;
using TalentX.Application.DTOs.Auth;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

public sealed record CreateEmployerCommand(
    string Name,
    string Email,
    string CompanyName,
    string Password,
    string ConfirmPassword
) : IRequest<AuthResponseDto>;
