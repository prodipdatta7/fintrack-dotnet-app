using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.SetAccountStatus;

// Accounts are never hard-deleted — transactions soft-reference AccountId,
// so DELETE is an alias for closing the account.
[ApiController]
[Route("api")]
public sealed class DeleteAccountController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteAccountController(ISender sender) => _sender = sender;

    [HttpDelete("delete-account/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new SetAccountStatusCommand(id, true), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
