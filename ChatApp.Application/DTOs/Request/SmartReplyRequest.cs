namespace ChatApp.Application.DTOs.Request;

public class SmartReplyRequest
{
    public Guid ConversationId { get; set; }
    public string LastMessage { get; set; } = null!;
}