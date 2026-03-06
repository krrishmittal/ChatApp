using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.DTOs.WebSocket;

namespace ChatApp.Application.Interfaces.Services;

public interface IMessageService
{
    Task<ApiResponse<MessageResponse>> SaveMessageAsync(
        Guid senderId, SendMessagePayload payload);
    Task<ApiResponse<PagedResponse<MessageResponse>>> GetMessagesAsync(
        Guid conversationId, Guid currentUserId, GetMessagesRequest request);
    Task<ApiResponse<bool>> MarkMessageAsReadAsync(
        Guid userId, Guid messageId);
    Task<List<Guid>> GetConversationParticipantIdsAsync(Guid conversationId);
    Task MarkMessageAsDeliveredAsync(Guid userId, Guid messageId);
    Task<List<MessageResponse>> GetPendingMessagesAsync(Guid userId); 
}