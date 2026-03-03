using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class MessageReciept : AuditableEntity
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public MessageStatus Status { get; set; }

    // Navigation
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}