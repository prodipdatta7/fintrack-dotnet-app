using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Users.Features.Register;

[ApiController]
[Route("api/users/auth")]
public sealed class RegisterController : ControllerBase
{
    private readonly ISender _sender;

    public RegisterController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), new { id = result.Value!.UserId }, result.Value)
            : BadRequest(new { error = result.Error });
    }
}
