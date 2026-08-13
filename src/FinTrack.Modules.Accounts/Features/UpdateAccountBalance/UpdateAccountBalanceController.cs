using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.UpdateAccountBalance;

[ApiController]
[Route("api")]
public sealed class UpdateAccountBalanceController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateAccountBalanceController(ISender sender) => _sender = sender;

    [HttpPatch("update-account-balance/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAccountBalance(
        [FromRoute] string id,
        [FromBody] BalanceRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateAccountBalanceCommand(id, request.Balance), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
