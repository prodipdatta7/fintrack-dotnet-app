using FinTrack.Modules.Accounts.Features.CreateAccount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Accounts.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ISender _sender;

    public AccountsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command, CancellationToken ct)
    {
        var accountId = await _sender.Send(command, ct);
        return Ok(new { AccountId = accountId });
    }
}
