using fintrack_netcore_app.Application.Features.Transactions.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace fintrack_netcore_app.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand command)
    {
        var transactionId = await _sender.Send(command);
        return Ok(new { TransactionId = transactionId });
    }
}

