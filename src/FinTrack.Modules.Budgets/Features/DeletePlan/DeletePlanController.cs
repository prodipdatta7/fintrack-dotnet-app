using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Features.DeletePlan;

[ApiController]
[Route("api")]
public sealed class DeletePlanController : ControllerBase
{
    private readonly ISender _sender;

    public DeletePlanController(ISender sender) => _sender = sender;

    [HttpDelete("delete-plan/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePlan([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeletePlanCommand(id), ct);

        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
