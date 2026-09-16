using MediatR;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.ApproveEmployer;

public sealed record ApproveEmployerCommand : IRequest
{
    public Guid EmployerId { get; init; }
}
