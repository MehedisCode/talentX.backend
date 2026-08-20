using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ISender Sender;

    protected BaseApiController(ISender sender)
    {
        Sender = sender;
    }
}
