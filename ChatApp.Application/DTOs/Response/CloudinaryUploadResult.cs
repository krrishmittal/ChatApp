namespace ChatApp.Application.DTOs.Response;

public class CloudinaryUploadResult
{
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;
}