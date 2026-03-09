using ChatApp.Application.DTOs.Response;
using Microsoft.AspNetCore.Http;

namespace ChatApp.Application.Interfaces.Services;
public interface ICloudinaryService
{
    Task<string>UploadImageAsync(IFormFile file);
    Task DeleteImageAsync(string publicId);
    Task<CloudinaryUploadResult> UploadFileAsync(IFormFile file, string folder = "chat-attachments");

}
