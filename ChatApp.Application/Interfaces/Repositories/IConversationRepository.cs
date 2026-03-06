using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetExistingConvesationAsync(Guid userAId, Guid userBId);
    Task<Conversation> CreateConversationAsync(Guid userAId, Guid userBId);
    Task<Conversation?> GetByIdAsync (Guid conversationId,Guid userId);
    Task<List<Conversation>> GetUserConversationsAsync(Guid userId);
    Task UpdateLastMessageAsync(Conversation conversation);

}
