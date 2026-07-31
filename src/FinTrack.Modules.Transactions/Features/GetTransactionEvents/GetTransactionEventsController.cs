using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.GetTransactionEvents;

[ApiController]
[Route("api/transactions")]
public sealed class GetTransactionEventsController : ControllerBase
{
    private readonly ISender _sender;

    public GetTransactionEventsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetTransactionEvents(string id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTransactionEventsQuery(id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
