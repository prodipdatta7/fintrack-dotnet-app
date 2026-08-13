using FinTrack.Modules.Budgets.Features.GetPlans;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Features.DepositToPlan;

[ApiController]
[Route("api")]
public sealed class DepositToPlanController : ControllerBase
{
    private readonly ISender _sender;

    public DepositToPlanController(ISender sender) => _sender = sender;

    [HttpPost("deposit-to-plan/{id}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DepositToPlan(
        [FromRoute] string id,
        [FromBody] DepositRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new DepositToPlanCommand(id, request.Amount), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
