namespace ChatApp.Application.DTOs.Response;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = null!;
    public bool IsSystemMessage { get; set; }
    public string Status { get; set; } = null!;
    public List<FileAttachmentResponse> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}