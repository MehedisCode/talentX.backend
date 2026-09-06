using MediatR;

namespace TalentX.Application.Features.SavedJobs.Queries.GetSavedJobIds;

public sealed record GetSavedJobIdsQuery() : IRequest<List<Guid>>;
