using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.ExecuteTool;
using FinTrack.Modules.Assistant.Features.ExtractTransactionFromReceipt;
using FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;
using FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;
using FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;
using FinTrack.Modules.Assistant.Features.GetSavingsPlansStatus;
using FinTrack.Modules.Assistant.Features.GetToolSchemas;
using FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;
using FinTrack.Modules.Assistant.Features.ProposeCreateAccount;
using FinTrack.Modules.Assistant.Features.ProposeCreateCategory;
using FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;
using FinTrack.Modules.Assistant.Features.ProposeCreateTag;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using FinTrack.Modules.Assistant.Features.ProposeTransfer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Assistant.Controllers;

[ApiController]
[Route("api/Assistant")]
[Authorize]
public sealed class AssistantToolsController : ControllerBase
{
    private readonly ISender _sender;

    public AssistantToolsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("GetToolSchemas")]
    [ProducesResponseType(typeof(IReadOnlyList<ToolDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToolSchemas(CancellationToken ct)
    {
        var result = await _sender.Send(new GetToolSchemasQuery(), ct);
        return Ok(result.Value);
    }

    [HttpPost("ExecuteTool")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteTool([FromBody] ExecuteToolCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ExtractTransactionFromReceipt")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateTransactionPayload>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractTransactionFromReceipt(
        [FromBody] ExtractTransactionFromReceiptCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProcessVoiceTurn")]
    [ProducesResponseType(typeof(Features.ProcessVoiceTurn.VoiceTurnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessVoiceTurn(
        [FromBody] Features.ProcessVoiceTurn.ProcessVoiceTurnCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("GetPortfolioOrAccountBalance")]
    [ProducesResponseType(typeof(PortfolioOrAccountBalanceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolioOrAccountBalance([FromBody] GetPortfolioOrAccountBalanceQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("GetActiveAccountsSummary")]
    [ProducesResponseType(typeof(ActiveAccountsSummaryResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAccountsSummary([FromBody] GetActiveAccountsSummaryQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("GetTopSpendingExpenses")]
    [ProducesResponseType(typeof(TopSpendingExpensesResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopSpendingExpenses([FromBody] GetTopSpendingExpensesQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("GetCategorySpendingVsBudget")]
    [ProducesResponseType(typeof(CategorySpendingVsBudgetResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategorySpendingVsBudget([FromBody] GetCategorySpendingVsBudgetQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("GetSavingsPlansStatus")]
    [ProducesResponseType(typeof(SavingsPlansStatusResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavingsPlansStatus([FromBody] GetSavingsPlansStatusQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeCreateTransaction")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateTransactionPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeCreateTransaction([FromBody] ProposeCreateTransactionCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeCreateAccount")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateAccountPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeCreateAccount([FromBody] ProposeCreateAccountCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeCreateCategory")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateCategoryPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeCreateCategory([FromBody] ProposeCreateCategoryCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeCreateTag")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateTagPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeCreateTag([FromBody] ProposeCreateTagCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeCreateSavingsPlan")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedCreateSavingsPlanPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeCreateSavingsPlan([FromBody] ProposeCreateSavingsPlanCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("ProposeTransfer")]
    [ProducesResponseType(typeof(ProposedActionDto<ProposedTransferPayload>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProposeTransfer([FromBody] ProposeTransferCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
