using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.GetCategoryTags;

[ApiController]
[Route("api")]
public sealed class GetCategoryTagsController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoryTagsController(ISender sender) => _sender = sender;

    [HttpGet("get-category-tags/{categoryId}")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategoryTags([FromRoute] string categoryId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCategoryTagsQuery(categoryId), ct);
        return Ok(result.Value);
    }
}
