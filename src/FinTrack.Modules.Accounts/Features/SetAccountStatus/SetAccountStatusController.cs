using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.SetAccountStatus;

[ApiController]
[Route("api")]
public sealed class SetAccountStatusController : ControllerBase
{
    private readonly ISender _sender;

    public SetAccountStatusController(ISender sender) => _sender = sender;

    [HttpPatch("update-account-status/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetAccountStatus(
        [FromRoute] string id,
        [FromBody] StatusRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new SetAccountStatusCommand(id, request.IsClosed), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
