using ChatApp.Application.Interfaces.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService>logger)
    {
        _logger = logger;
        var section = config.GetSection("Cloudinary");
        var account = new Account(
            section["CloudName"],
            section["ApiKey"],
            section["ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }
    public async Task DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
        _logger.LogInformation("Image deleted from Cloudinary: {PublicId}", publicId);
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        _logger.LogInformation("Uploading image: {FileName}", file.FileName);
        await using var stream = file.OpenReadStream();
        var uploadParams= new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "chat-app",
            PublicId = Guid.NewGuid().ToString()
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        if(result.Error != null)
        {
            _logger.LogError("Cloudinary upload error: {ErrorMessage}", result.Error.Message);
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
        }
        _logger.LogInformation("Image uploaded: {Url}", result.SecureUrl);

        return result.SecureUrl.ToString();
    }
}