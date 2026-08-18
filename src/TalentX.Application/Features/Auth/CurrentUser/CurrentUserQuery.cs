using MediatR;
using TalentX.Application.Identity.Models;

namespace TalentX.Application.Features.Auth.CurrentUser;

public record CurrentUserQuery : IRequest<IdentityResponse<CurrentUserResult>>;