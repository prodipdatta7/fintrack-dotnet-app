using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.GetTags;

[ApiController]
[Route("api")]
public sealed class GetTagsController : ControllerBase
{
    private readonly ISender _sender;

    public GetTagsController(ISender sender) => _sender = sender;

    [HttpGet("get-tags")]
    [ProducesResponseType(typeof(List<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTags(CancellationToken ct)
    {
        var result = await _sender.Send(new GetTagsQuery(), ct);
        return Ok(result.Value);
    }
}
