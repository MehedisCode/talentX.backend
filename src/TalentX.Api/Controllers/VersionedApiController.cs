using Microsoft.AspNetCore.Mvc;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class VersionedApiController : BaseApiController
{
}
