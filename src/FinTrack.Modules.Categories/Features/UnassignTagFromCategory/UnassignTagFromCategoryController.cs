using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.UnassignTagFromCategory;

[ApiController]
[Route("api")]
public sealed class UnassignTagFromCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public UnassignTagFromCategoryController(ISender sender) => _sender = sender;

    [HttpDelete("unassign-tag-from-category/{categoryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnassignTagFromCategory(
        [FromRoute] string categoryId,
        [FromQuery] string tag,
        CancellationToken ct)
    {
        var result = await _sender.Send(new UnassignTagFromCategoryCommand(categoryId, tag), ct);

        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }
}
