using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.CreateCategory;

[ApiController]
[Route("api/categories")]
public sealed class CreateCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public CreateCategoryController(ISender sender) => _sender = sender;

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateCategory), new { id = result.Value }, new { categoryId = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
