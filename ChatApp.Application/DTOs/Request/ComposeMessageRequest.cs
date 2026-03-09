namespace ChatApp.Application.DTOs.Request;

public class ComposeMessageRequest
{
    public string Prompt { get; set; } = null!;       
    public string? ConversationContext { get; set; }   
}