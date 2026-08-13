using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Features.UpdatePlan;

[ApiController]
[Route("api")]
public sealed class UpdatePlanController : ControllerBase
{
    private readonly ISender _sender;

    public UpdatePlanController(ISender sender) => _sender = sender;

    [HttpPut("update-plan/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePlan(
        [FromRoute] string id,
        [FromBody] UpdatePlanCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest(new { error = "Route id does not match body id." });

        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
