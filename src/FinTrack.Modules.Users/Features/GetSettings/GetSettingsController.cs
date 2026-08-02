using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.GetSettings;

[ApiController]
[Route("api/users")]
public sealed class GetSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public GetSettingsController(ISender sender) => _sender = sender;

    [HttpGet("me/settings")]
    [ProducesResponseType(typeof(GetSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var result = await _sender.Send(new GetSettingsQuery(), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
