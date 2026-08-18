using MediatR;
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Features.Auth.CurrentUser;

public record CurrentUserQuery : IRequest<IdentityResponse<CurrentUserResult>>;