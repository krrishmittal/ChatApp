namespace ChatApp.Application.DTOs.WebSocket;
public class TypingPayload
{
    public Guid ConversationId { get; set; }
    public bool IsTyping { get; set; }

}