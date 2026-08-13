using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.CreateAccount;

[ApiController]
[Route("api")]
public sealed class CreateAccountController : ControllerBase
{
    private readonly ISender _sender;

    public CreateAccountController(ISender sender) => _sender = sender;

    [HttpPost("create-account")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateAccount), new { id = result.Value }, new { accountId = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
