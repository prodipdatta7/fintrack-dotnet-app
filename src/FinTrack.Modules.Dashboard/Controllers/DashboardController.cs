using FinTrack.Modules.Dashboard.Features.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Dashboard.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }
}
