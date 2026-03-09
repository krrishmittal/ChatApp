namespace ChatApp.Application.DTOs.Request;

public class TranslateRequest
{
    public string Message { get; set; } = null!;
    public string TargetLanguage { get; set; } = "English";
}