namespace ChatApp.Domain.Entities;

public class Conversation : AuditableEntity
{
    public Guid? LastMessageId { get; set; }

    // Navigation
    public Message? LastMessage { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}