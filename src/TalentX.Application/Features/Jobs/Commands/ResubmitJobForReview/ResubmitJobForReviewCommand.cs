using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;

public sealed record ResubmitJobForReviewCommand(Guid Id) : IRequest<Unit>;
