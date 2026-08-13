using FinTrack.Modules.Accounts.Features.GetAccounts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.GetAccount;

[ApiController]
[Route("api")]
public sealed class GetAccountController : ControllerBase
{
    private readonly ISender _sender;

    public GetAccountController(ISender sender) => _sender = sender;

    [HttpGet("get-account/{id}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccount([FromRoute] string id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAccountQuery(id), ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
