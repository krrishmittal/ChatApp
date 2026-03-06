using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;

namespace ChatApp.Application.Interfaces.Services;
public interface IConversationService
{
    Task<ApiResponse<ConversationResponse>> CreateOrGetConversationAsync(Guid currentUserId, CreateConversationRequest request);
    Task<ApiResponse<List<ConversationResponse>>>GetMyConversationsAsync(Guid currentUserId);   
}