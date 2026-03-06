namespace ChatApp.Application.DTOs.Response;

public class FileAttachmentResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;
}