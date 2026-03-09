namespace ChatApp.Application.DTOs.Request;

public class ScheduleMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }  
}