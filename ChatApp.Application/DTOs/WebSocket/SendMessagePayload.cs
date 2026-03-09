namespace ChatApp.Application.DTOs.WebSocket;
public class SendMessagePayload
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = null!;
    public List<Guid>? AttachmentIds { get; set; }
    public DateTime? ScheduledAt { get; set; } 

}
