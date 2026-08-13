using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.UploadAvatar;

[ApiController]
[Route("api")]
public sealed class UploadAvatarController : ControllerBase
{
    private readonly ISender _sender;

    public UploadAvatarController(ISender sender) => _sender = sender;

    [HttpPost("upload-avatar")]
    [ProducesResponseType(typeof(UploadAvatarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Please select a valid avatar file to upload." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Invalid file type. Only JPG, PNG, WEBP, and GIF images are allowed." });

        if (file.Length > 5 * 1024 * 1024) // 5 MB limit
            return BadRequest(new { error = "File size exceeds maximum allowed limit of 5 MB." });

        using var stream = file.OpenReadStream();
        var command = new UploadAvatarCommand(stream, file.FileName, file.ContentType);

        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
