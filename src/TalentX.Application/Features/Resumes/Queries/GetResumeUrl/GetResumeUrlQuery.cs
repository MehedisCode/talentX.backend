using MediatR;

namespace TalentX.Application.Features.Resumes.Queries.GetResumeUrl;

public sealed record GetResumeUrlQuery(Guid Id) : IRequest<GetResumeUrlResponse>;
