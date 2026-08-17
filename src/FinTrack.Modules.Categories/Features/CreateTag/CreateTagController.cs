using FinTrack.Modules.Categories.Features.GetTags;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.CreateTag;

[ApiController]
[Route("api")]
public sealed class CreateTagController : ControllerBase
{
    private readonly ISender _sender;

    public CreateTagController(ISender sender) => _sender = sender;

    [HttpPost("create-tag")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
