namespace ChatApp.Application.DTOs.WebSocket;
public class ReadReceiptPayload
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
}