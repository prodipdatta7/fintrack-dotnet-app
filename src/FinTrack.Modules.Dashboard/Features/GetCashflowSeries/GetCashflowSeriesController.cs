using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

[ApiController]
[Route("api")]
public sealed class GetCashflowSeriesController : ControllerBase
{
    private readonly ISender _sender;

    public GetCashflowSeriesController(ISender sender) => _sender = sender;

    [HttpGet("get-cashflow")]
    [ProducesResponseType(typeof(IReadOnlyList<CashflowPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCashflow(
        [FromQuery] string timeframe = "30D",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? accountId = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCashflowSeriesQuery(timeframe, from, to, accountId), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
