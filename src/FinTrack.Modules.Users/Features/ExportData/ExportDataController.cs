using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.ExportData;

[ApiController]
[Route("api")]
public sealed class ExportDataController : ControllerBase
{
    private readonly ISender _sender;

    public ExportDataController(ISender sender) => _sender = sender;

    [HttpPost("export-data")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportData([FromBody] ExportDataCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        if (!result.IsSuccess || result.Value is null)
            return BadRequest(new { error = result.Error ?? "Failed to export data." });

        return File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName);
    }
}
