using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected readonly ISender Sender;

    protected BaseController(ISender sender)
    {
        Sender = sender;
    }
}
