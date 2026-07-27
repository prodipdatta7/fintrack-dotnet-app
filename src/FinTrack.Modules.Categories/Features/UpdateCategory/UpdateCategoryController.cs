using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.UpdateCategory;

[ApiController]
[Route("api/categories")]
public sealed class UpdateCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public UpdateCategoryController(ISender sender) => _sender = sender;

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] string id,
        [FromBody] UpdateCategoryCommand command,
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
