namespace ChatApp.Domain.Entities;
public class Message: AuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsSystemMessage { get; set; }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<MessageReciept>Reciepts { get; set; } = new List<MessageReciept>();
    public ICollection<FileAttachment>Attachments { get; set; } = new List<FileAttachment>();

}