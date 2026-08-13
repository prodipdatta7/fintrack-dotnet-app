using FinTrack.BuildingBlocks.Pagination;
using FinTrack.Modules.Transactions.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Transactions.Features.GetTransactions;

[ApiController]
[Route("api")]
public sealed class GetTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public GetTransactionsController(ISender sender) => _sender = sender;

    [HttpGet("get-transactions")]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TransactionType? type = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? accountId = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        CancellationToken ct = default)
    {
        var query = new GetTransactionsQuery(
            page, pageSize, type, categoryId, fromDate, toDate,
            accountId, minAmount, maxAmount, searchTerm, sortBy);
        var result = await _sender.Send(query, ct);

        return Ok(result.Value);
    }
}
