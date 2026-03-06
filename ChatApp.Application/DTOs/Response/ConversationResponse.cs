namespace ChatApp.Application.DTOs.Response;
public class ConversationResponse
{
    public Guid Id { get; set;}
    public UserResponse OtherUser { get; set;}
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
