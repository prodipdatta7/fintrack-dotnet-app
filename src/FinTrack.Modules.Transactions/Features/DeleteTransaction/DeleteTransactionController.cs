using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.DeleteTransaction;

[ApiController]
[Route("api/transactions")]
public sealed class DeleteTransactionController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteTransactionController(ISender sender) => _sender = sender;

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTransaction([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteTransactionCommand(id), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
