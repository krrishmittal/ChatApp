using System.Security.Claims;
using ChatApp.Application.DTOs.Request;
using ChatApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[ApiController]
[Route("api/conversations/{conversationId}/messages")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
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
}