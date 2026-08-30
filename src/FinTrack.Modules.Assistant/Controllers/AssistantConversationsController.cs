using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.Conversations.CreateConversation;
using FinTrack.Modules.Assistant.Features.Conversations.DeleteConversation;
using FinTrack.Modules.Assistant.Features.Conversations.GetConversation;
using FinTrack.Modules.Assistant.Features.Conversations.GetConversations;
using FinTrack.Modules.Assistant.Features.Conversations.GetMessages;
using FinTrack.Modules.Assistant.Features.Conversations.SendMessage;
using FinTrack.Modules.Assistant.Features.Conversations.UpdateConversationTitle;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Modules.Assistant.Controllers;

[ApiController]
[Route("api/Assistant")]
[Authorize]
public sealed class AssistantConversationsController : ControllerBase
{
    private readonly ISender _sender;

    public AssistantConversationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("GetConversations")]
    [ProducesResponseType(typeof(ConversationListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new GetConversationsQuery(page, pageSize, searchTerm);
        var result = await _sender.Send(query, ct);
        return Ok(result.Value);
    }

    [HttpPost("CreateConversation")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetConversation), new { conversationId = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("GetConversation/{conversationId}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] string conversationId,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetConversationQuery(conversationId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPatch("UpdateConversationTitle/{conversationId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConversationTitle(
        [FromRoute] string conversationId,
        [FromBody] UpdateConversationTitleRequest request,
        CancellationToken ct)
    {
        var command = new UpdateConversationTitleCommand(conversationId, request.Title);
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(new { title = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("TogglePinConversation/{conversationId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TogglePinConversation(
        [FromRoute] string conversationId,
        CancellationToken ct)
    {
        var command = new Features.Conversations.TogglePinConversation.TogglePinConversationCommand(conversationId);
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(new { isPinned = result.Value }) : NotFound(new { error = result.Error });
    }

    [HttpDelete("DeleteConversation/{conversationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(
        [FromRoute] string conversationId,
        CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteConversationCommand(conversationId), ct);
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }

    [HttpGet("GetMessages/{conversationId}")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string conversationId,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetMessagesQuery(conversationId, limit), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("SendMessage")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

public sealed record UpdateConversationTitleRequest(string Title);
