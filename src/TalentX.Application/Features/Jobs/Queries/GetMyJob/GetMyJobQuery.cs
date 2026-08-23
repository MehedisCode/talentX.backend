using MediatR;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetMyJob;

public record GetMyJobQuery(Guid Id) : IRequest<MyJobDto>;
