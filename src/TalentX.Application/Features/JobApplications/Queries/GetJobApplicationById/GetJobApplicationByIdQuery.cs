using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicationById;

public record GetJobApplicationByIdQuery(Guid Id) : IRequest<JobApplicationDetailsDto>;
