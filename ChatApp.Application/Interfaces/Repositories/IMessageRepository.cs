using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<Message> CreateMessageAsync(Message message);
    Task<List<Message>> GetConversationMessagesAsync(
        Guid conversationId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
    Task UpdateMessageStatusAsync(Guid messageId, Guid userId, MessageStatus status);
    Task<Message?> GetByIdAsync(Guid messageId);
    Task<List<Guid>> GetConversationParticipantIdsAsync(Guid conversationId);
    Task<int> GetTotalMessagesCountAsync(Guid conversationId);
    Task<List<Message>> GetPendingMessagesAsync(Guid userId);
    Task MarkAllAsReadAsync(Guid conversationId, Guid userId); // ← add this
}