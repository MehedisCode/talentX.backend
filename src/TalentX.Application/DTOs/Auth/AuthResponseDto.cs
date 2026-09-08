using TalentX.Application.DTOs.Employers;

namespace TalentX.Application.DTOs.Auth;

public sealed record AuthResponseDto(
    EmployerDto? Employer,
    string AccessToken,
    string RefreshToken
);
