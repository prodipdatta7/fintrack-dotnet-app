using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Features.CreatePlan;

[ApiController]
[Route("api")]
public sealed class CreatePlanController : ControllerBase
{
    private readonly ISender _sender;

    public CreatePlanController(ISender sender) => _sender = sender;

    [HttpPost("create-plan")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreatePlanCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(CreatePlan), new { id = result.Value }, new { planId = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
