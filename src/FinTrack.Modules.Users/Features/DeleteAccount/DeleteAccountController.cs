using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.DeleteAccount;

[ApiController]
[Route("api/users")]
public sealed class DeleteAccountController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteAccountController(ISender sender) => _sender = sender;

    [HttpPost("me/delete-account")]
    [ProducesResponseType(typeof(DeleteAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
