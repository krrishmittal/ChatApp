namespace ChatApp.Domain.Entities;

public class FileAttachment : AuditableEntity
{
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;

    // Navigation
    public Message Message { get; set; } = null!;
}