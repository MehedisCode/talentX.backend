using MediatR;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed record DeleteAccountCommand(string Otp) : IRequest<DeleteAccountResult>;
