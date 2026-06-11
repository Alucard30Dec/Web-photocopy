using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebPhotocopyHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/system")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public sealed class SystemApiController : ControllerBase
{
    [HttpGet("ping")]
    [ProducesResponseType(typeof(SystemPingResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemPingResponse> Ping()
    {
        return Ok(new SystemPingResponse(
            IsSuccess: true,
            Message: "WebPhotocopyHub API is running.",
            ServerTimeUtc: DateTime.UtcNow));
    }
}

public sealed record SystemPingResponse(
    bool IsSuccess,
    string Message,
    DateTime ServerTimeUtc);
