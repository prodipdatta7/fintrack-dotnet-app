using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.GetMe;

[ApiController]
[Route("api/users")]
public sealed class GetMeController : ControllerBase
{
    private readonly ISender _sender;

    public GetMeController(ISender sender) => _sender = sender;

    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await _sender.Send(new GetMeQuery(), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
