using FinTrack.Modules.Categories.Features.GetCategories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.GetCategory;

[ApiController]
[Route("api")]
public sealed class GetCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoryController(ISender sender) => _sender = sender;

    [HttpGet("get-category/{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategory([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCategoryQuery(id), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
