using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.UpdateAccount;

[ApiController]
[Route("api")]
public sealed class UpdateAccountController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateAccountController(ISender sender) => _sender = sender;

    [HttpPut("update-account/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAccount(
        [FromRoute] string id,
        [FromBody] UpdateAccountCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest(new { error = "Route id does not match body id." });

        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
