namespace ChatApp.Application.DTOs.WebSocket;
public class SendMessagePayload
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = null!;
}
