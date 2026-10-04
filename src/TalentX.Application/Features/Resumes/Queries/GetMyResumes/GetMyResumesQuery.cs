using MediatR;

namespace TalentX.Application.Features.Resumes.Queries.GetMyResumes;

public sealed record GetMyResumesQuery : IRequest<GetMyResumesResponse>;
