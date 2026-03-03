namespace ChatApp.Domain.Entities;

public class ConversationParticipant : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsMuted { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}