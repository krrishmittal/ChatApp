using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IMessageService _messageService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiService aiService,
        IMessageService messageService,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _messageService = messageService;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpPost("smart-replies")]
    public async Task<IActionResult> GetSmartReplies(
        [FromBody] SmartReplyRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        _logger.LogInformation(
            "Smart reply request for ConversationId={ConvId}", request.ConversationId);

        // Get last 5 messages as context
        var messagesResult = await _messageService.GetMessagesAsync(
            request.ConversationId, userId.Value,
            new GetMessagesRequest { Page = 1, PageSize = 5 });

        var context = messagesResult.Success
            ? string.Join("\n", messagesResult.Data!.Items
                .Select(m => $"{m.SenderName}: {m.Content}"))
            : string.Empty;

        var suggestions = await _aiService.GetSmartRepliesAsync(
            request.LastMessage, context);

        return Ok(ApiResponse<SmartReplyResponse>.Ok(
            new SmartReplyResponse { Suggestions = suggestions }));
    }

    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        _logger.LogInformation("Translation request to {Language}", request.TargetLanguage);

        var translated = await _aiService.TranslateMessageAsync(
            request.Message, request.TargetLanguage);

        return Ok(ApiResponse<AiTextResponse>.Ok(
            new AiTextResponse { Result = translated }));
    }

    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        _logger.LogInformation(
            "Summarize request for ConversationId={ConvId}", request.ConversationId);

        // Get last 50 messages for summarization
        var messagesResult = await _messageService.GetMessagesAsync(
            request.ConversationId, userId.Value,
            new GetMessagesRequest { Page = 1, PageSize = 50 });

        if (!messagesResult.Success || !messagesResult.Data!.Items.Any())
            return BadRequest(ApiResponse<AiTextResponse>.Fail(
                "No messages found to summarize.", 400, nameof(Summarize)));

        var messages = messagesResult.Data.Items
            .Select(m => $"{m.SenderName}: {m.Content}")
            .ToList();

        var summary = await _aiService.SummarizeConversationAsync(messages);

        return Ok(ApiResponse<AiTextResponse>.Ok(
            new AiTextResponse { Result = summary }));
    }

    [HttpPost("compose")]
    public async Task<IActionResult> ComposeMessage([FromBody] ComposeMessageRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        _logger.LogInformation("Compose request: {Prompt}", request.Prompt);

        var composed = await _aiService.ComposeMessageAsync(
            request.Prompt, request.ConversationContext);

        return Ok(ApiResponse<AiTextResponse>.Ok(
            new AiTextResponse { Result = composed }));
    }
}