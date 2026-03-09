using ChatApp.Application.DTOs.Request;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.API.Controllers;

[ApiController]
[Route("api/conversations/{conversationId}/messages")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly WebSocketNotifier _webSocketNotifier; 


    public MessageController(IMessageService messageService,WebSocketNotifier webSocketNotifier)
    {
        _messageService = messageService;
        _webSocketNotifier = webSocketNotifier;

    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(
        Guid conversationId, [FromQuery] GetMessagesRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _messageService.GetMessagesAsync(
            conversationId, userId, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SendFileMessage(
    [FromRoute] Guid conversationId, 
    [FromForm] SendFileMessageRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _messageService.SendFileMessageAsync(userId.Value,conversationId, request);

        if (!result.Success)
            return BadRequest(result);

        await _webSocketNotifier.NotifyNewMessageAsync(result.Data!, userId.Value);

        return Ok(result);
    }
    protected Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}