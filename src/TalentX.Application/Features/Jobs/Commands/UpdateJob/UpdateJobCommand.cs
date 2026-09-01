using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public record UpdateJobCommand(
    Guid Id,
    string? Title,
    string? CompanyName,
    string? Description,
    EmploymentType? EmploymentType,
    ExperienceLevel? ExperienceLevel,
    string? Location,
    WorkArrangement? WorkArrangement,
    List<string>? RequiredSkills,
    int? Vacancies,
    DateTime? ExpireAt,
    bool Publish = false,
    string? City = null,
    string? Country = null,
    short? MinOverlapHours = null
) : IRequest;
