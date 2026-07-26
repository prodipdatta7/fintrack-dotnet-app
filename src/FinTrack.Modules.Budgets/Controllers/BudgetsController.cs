using FinTrack.Modules.Budgets.Features.CreateBudget;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Budgets.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly ISender _sender;

    public BudgetsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetCommand command, CancellationToken ct)
    {
        var budgetId = await _sender.Send(command, ct);
        return Ok(new { BudgetId = budgetId });
    }
}
