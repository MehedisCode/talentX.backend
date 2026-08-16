using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Jobs.Commands.CreateJob;

public record CreateJobCommand(
    string Title,
    string Description,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    string Location,
    WorkArrangement WorkArrangement,
    List<string> RequiredSkills,
    int Vacancies,
    bool Publish
) : IRequest<Guid>;
