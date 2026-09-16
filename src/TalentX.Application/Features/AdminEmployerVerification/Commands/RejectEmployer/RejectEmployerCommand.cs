using MediatR;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.RejectEmployer;

public sealed record RejectEmployerCommand : IRequest
{
    public Guid EmployerId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
