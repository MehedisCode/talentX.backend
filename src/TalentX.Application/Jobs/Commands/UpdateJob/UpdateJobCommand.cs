using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Jobs.Commands.UpdateJob;

public record UpdateJobCommand(
    Guid Id,
    string? Title,
    string? Description,
    EmploymentType? EmploymentType,
    ExperienceLevel? ExperienceLevel,
    string? Location,
    WorkArrangement? WorkArrangement,
    List<string>? RequiredSkills,
    int? Vacancies,
    bool Publish = false
) : IRequest;
