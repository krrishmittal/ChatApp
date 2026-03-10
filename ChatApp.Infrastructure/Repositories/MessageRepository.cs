using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Repositories;
public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MessageRepository> _logger;
    public MessageRepository(AppDbContext dbContext, ILogger<MessageRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task<Message> CreateMessageAsync(Message message)
    {
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();
        return await _dbContext.Messages
            .Include(m=>m.Sender)
            .Include(m=>m.Attachments)
            .Include(m=>m.Reciepts)
            .AsSplitQuery()
            .FirstAsync(m=>m.Id==message.Id);
    }

    public async Task<List<Message>> GetConversationMessagesAsync(Guid conversationId, int page, int pageSize)
    {
        var messages = await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reciepts)
            .AsSplitQuery()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages.OrderBy(m => m.CreatedAt).ToList();
    }
    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        return await _dbContext.MessageReciepts
            .Where(r =>
                r.Message.ConversationId == conversationId &&
                r.UserId == userId &&
                r.Status != MessageStatus.Read)
            .CountAsync();
    }
    public async Task UpdateMessageStatusAsync(Guid messageId, Guid userId, MessageStatus status)
    {
        var receipt = await _dbContext.MessageReciepts
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (receipt is null) return;

        if ((int)receipt.Status < (int)status)
        {
            receipt.Status = status;
            receipt.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("MessageId={MessageId} status updated to {Status} for UserId={UserId}",
                messageId, status, userId);
        }
    }
    public async Task<Message?> GetByIdAsync(Guid messageId)
    {
        return await _dbContext.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reciepts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
    public async Task<List<Guid>> GetConversationParticipantIdsAsync(Guid conversationId)
    {
        return await _dbContext.ConversationParticipants
            .Where(p => p.ConversationId == conversationId)
            .Select(p => p.UserId)
            .ToListAsync();
    }

    public async Task<int> GetTotalMessagesCountAsync(Guid conversationId)
    {
        return await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .CountAsync();
    }


    public async Task<List<Message>> GetPendingMessagesAsync(Guid userId)
    {
        return await _dbContext.MessageReciepts
            .Where(r =>
                r.UserId == userId &&
                r.Status == MessageStatus.Sent)
            .Include(r => r.Message)
                .ThenInclude(m => m.Sender)
            .Include(r => r.Message)
                .ThenInclude(m => m.Attachments)
            .Include(r => r.Message)
                .ThenInclude(m => m.Reciepts)
            .AsSplitQuery()
            .Select(r => r.Message)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAllAsReadAsync(Guid conversationId, Guid userId)
    {
        var unreadReceipts = await _dbContext.MessageReciepts
            .Where(r =>
                r.Message.ConversationId == conversationId &&
                r.UserId == userId &&
                r.Status != MessageStatus.Read)
            .ToListAsync();

        if (!unreadReceipts.Any()) return;

        foreach (var receipt in unreadReceipts)
        {
            receipt.Status = MessageStatus.Read;
            receipt.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} messages as read for UserId={UserId} in ConversationId={ConvId}",
            unreadReceipts.Count, userId, conversationId);
    }
}
