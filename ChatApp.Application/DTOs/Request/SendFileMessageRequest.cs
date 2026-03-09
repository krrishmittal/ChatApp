using Microsoft.AspNetCore.Http;

namespace ChatApp.Application.DTOs.Request;

public class SendFileMessageRequest
{
    public string? Content { get; set; } 
    public IFormFile File { get; set; } = null!;
}