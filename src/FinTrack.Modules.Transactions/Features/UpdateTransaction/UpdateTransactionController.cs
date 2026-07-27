using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.UpdateTransaction;

[ApiController]
[Route("api/transactions")]
public sealed class UpdateTransactionController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateTransactionController(ISender sender) => _sender = sender;

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTransaction(
        [FromRoute] string id,
        [FromBody] UpdateTransactionCommand command,
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
