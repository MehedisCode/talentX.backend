using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.CreateJob;

public record CreateJobCommand(
    string Title,
    string CompanyName,
    string Description,
    EmploymentType EmploymentType,
    ExperienceLevel ExperienceLevel,
    string Location,
    WorkArrangement WorkArrangement,
    List<string> RequiredSkills,
    int Vacancies,
    DateTime ExpireAt,
    bool Publish,
    string? City = null,
    string? Country = null,
    short? MinOverlapHours = null
) : IRequest<Guid>;
