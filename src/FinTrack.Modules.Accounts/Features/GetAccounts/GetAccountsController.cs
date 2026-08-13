using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Features.GetAccounts;

[ApiController]
[Route("api")]
public sealed class GetAccountsController : ControllerBase
{
    private readonly ISender _sender;

    public GetAccountsController(ISender sender) => _sender = sender;

    [HttpGet("get-accounts")]
    [ProducesResponseType(typeof(AccountListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] bool includeClosed = false,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAccountsQuery(includeClosed), ct);
        return Ok(result.Value);
    }
}
