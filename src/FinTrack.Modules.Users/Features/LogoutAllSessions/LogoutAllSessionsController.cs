using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.LogoutAllSessions;

[ApiController]
[Route("api/users")]
public sealed class LogoutAllSessionsController : ControllerBase
{
    private readonly ISender _sender;

    public LogoutAllSessionsController(ISender sender) => _sender = sender;

    [HttpPost("me/logout-all")]
    [ProducesResponseType(typeof(LogoutAllSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
    {
        var result = await _sender.Send(new LogoutAllSessionsCommand(), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
