using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Dashboard.Features.GetDashboardSummary;

[ApiController]
[Route("api")]
public sealed class GetDashboardSummaryController : ControllerBase
{
    private readonly ISender _sender;

    public GetDashboardSummaryController(ISender sender) => _sender = sender;

    [HttpGet("get-dashboard-summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? accountId = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(from, to, accountId), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
