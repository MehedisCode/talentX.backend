using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Queries.GetLocalFileStream;

public sealed record GetLocalFileStreamQuery(string StorageKey) : IRequest<LocalFileStreamResult>;
