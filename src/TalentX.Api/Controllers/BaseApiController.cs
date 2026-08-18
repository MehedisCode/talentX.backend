using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TalentX.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _sender;
    protected ISender Mediator => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
