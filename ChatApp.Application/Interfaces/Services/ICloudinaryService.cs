using Microsoft.AspNetCore.Http;

namespace ChatApp.Application.Interfaces.Services;
public interface ICloudinaryService
{
    Task<string>UploadImageAsync(IFormFile file);
    Task DeleteImageAsync(string publicId);
}
