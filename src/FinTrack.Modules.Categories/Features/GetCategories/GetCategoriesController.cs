using FinTrack.Modules.Categories.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Categories.Features.GetCategories;

[ApiController]
[Route("api/categories")]
public sealed class GetCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoriesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] CategoryType? type = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCategoriesQuery(type), ct);
        return Ok(result.Value);
    }
}
