using ChatApp.Application.DTOs.Request;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrGetConversation([FromBody] CreateConversationRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _conversationService.CreateOrGetConversationAsync(userId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyConversations()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _conversationService.GetMyConversationsAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
