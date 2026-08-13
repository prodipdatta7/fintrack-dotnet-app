using FinTrack.Modules.Transactions.Features.GetTransactions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.GetTransaction;

[ApiController]
[Route("api")]
public sealed class GetTransactionController : ControllerBase
{
    private readonly ISender _sender;

    public GetTransactionController(ISender sender) => _sender = sender;

    [HttpGet("get-transaction/{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransaction([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetTransactionQuery(id), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
