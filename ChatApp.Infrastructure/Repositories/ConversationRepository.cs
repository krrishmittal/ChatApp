using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Repositories;
public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ConversationRepository> _logger;
    public ConversationRepository(AppDbContext dbContext, ILogger<ConversationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task<Conversation?> GetExistingConvesationAsync(Guid userAId, Guid userBId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
                .ThenInclude(m => m.Reciepts)
            .Where(c=>
                c.Participants.Any(p => p.UserId == userAId) &&
                c.Participants.Any(p => p.UserId == userBId) &&
                c.Participants.Count == 2
            )
            .FirstOrDefaultAsync();
    }
    public async Task<Conversation> CreateConversationAsync(Guid userAId, Guid userBId)
    {
        var conversation = new Conversation
        {
            CreatedAt = DateTime.UtcNow,
        };
        conversation.Participants.Add(new ConversationParticipant
        {
            UserId= userAId,
            CreatedAt= DateTime.UtcNow,
        });
        conversation.Participants.Add(new ConversationParticipant
        {
            UserId = userBId,
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync();

        return await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == conversation.Id);
    }
    public async Task<Conversation?> GetByIdAsync(Guid conversationId, Guid currentUserId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
                .ThenInclude(m => m.Reciepts)
            .Where(c =>
                c.Id == conversationId &&
                c.Participants.Any(p => p.UserId == currentUserId))
            .FirstOrDefaultAsync();
    }
    public async Task<List<Conversation>> GetUserConversationsAsync(Guid userId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
                .ThenInclude(m => m.Reciepts)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.LastMessage != null
                ? c.LastMessage.CreatedAt
                : c.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateLastMessageAsync(Conversation conversation)
    {
        _dbContext.Conversations.Update(conversation);
        await _dbContext.SaveChangesAsync();
    }
}
