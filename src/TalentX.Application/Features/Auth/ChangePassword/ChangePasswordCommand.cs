using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(string OldPassword, string NewPassword) 
    : IRequest<IdentityResponse>;
