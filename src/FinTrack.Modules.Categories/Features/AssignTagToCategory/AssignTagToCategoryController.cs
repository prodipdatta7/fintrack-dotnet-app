using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.AssignTagToCategory;

[ApiController]
[Route("api")]
public sealed class AssignTagToCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public AssignTagToCategoryController(ISender sender) => _sender = sender;

    [HttpPost("assign-tag-to-category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignTagToCategory(
        [FromBody] AssignTagToCategoryCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }
}
