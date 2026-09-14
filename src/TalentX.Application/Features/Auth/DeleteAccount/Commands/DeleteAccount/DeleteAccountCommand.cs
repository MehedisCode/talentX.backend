using MediatR;
using TalentX.Application.Features.Auth.DeleteAccount.DTOs;

namespace TalentX.Application.Features.Auth.DeleteAccount.Commands.DeleteAccount;

public sealed record DeleteAccountCommand(string Otp) : IRequest<DeleteAccountResult>;
