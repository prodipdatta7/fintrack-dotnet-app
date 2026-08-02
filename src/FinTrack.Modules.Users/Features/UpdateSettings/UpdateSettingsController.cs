using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.UpdateSettings;

[ApiController]
[Route("api/users")]
public sealed class UpdateSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateSettingsController(ISender sender) => _sender = sender;

    [HttpPut("me/settings")]
    [ProducesResponseType(typeof(UpdateSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
