using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TalentX.API.Controllers;

[ApiController]
public class BaseApiController : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}