using MediatR;
using TalentX.Application.DTOs.Employers;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

/// <summary>
/// Command to register and create a new employer account and profile.
/// </summary>
/// <param name="Name">The full name of the employer.</param>
/// <param name="Email">The email address of the employer.</param>
/// <param name="CompanyName">The company or organization name.</param>
/// <param name="Password">The account password.</param>
/// <param name="ConfirmPassword">The password confirmation string.</param>
public sealed record CreateEmployerCommand(
    string Name,
    string Email,
    string CompanyName,
    string Password,
    string ConfirmPassword
) : IRequest<EmployerDto>;
