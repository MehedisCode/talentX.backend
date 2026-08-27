using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;

public sealed record ListMediaMultipartPartsQuery(
    string StorageKey,
    string UploadId
) : IRequest<IReadOnlyList<UploadedPartDto>>;
