using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

[ApiController]
[Route("api/transactions")]
public sealed class CreateTransactionController : ControllerBase
{
    private readonly ISender _sender;

    public CreateTransactionController(ISender sender) => _sender = sender;

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateTransaction), new { id = result.Value }, new { transactionId = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
