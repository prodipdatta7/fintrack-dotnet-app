using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Features.GetPlans;

[ApiController]
[Route("api")]
public sealed class GetPlansController : ControllerBase
{
    private readonly ISender _sender;

    public GetPlansController(ISender sender) => _sender = sender;

    [HttpGet("get-plans")]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPlansQuery(), ct);
        return Ok(result.Value);
    }
}
