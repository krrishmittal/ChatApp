using ChatApp.Application.DTOs.Response;
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

    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        //images
        "image/jpeg","image/png","image/gif","image/webp",
        //videos
        "video/mp4","video/webm","video/quicktime",
        //documents
        "application/pdf","application/msword","application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        //audio
        "audio/mpeg","audio/wav","audio/ogg"

    };
    private const long MaxFileSize = 10 * 1024 * 1024; 

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

    public async Task<CloudinaryUploadResult> UploadFileAsync(IFormFile file, string folder = "chat-attachments")
    {
        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("File size exceeds limit: {FileName} ({FileSize} bytes)", file.FileName, file.Length);
            throw new Exception($"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)} MB.");

        }
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Unsupported file type: {FileName} ({ContentType})", file.FileName, file.ContentType);
            throw new Exception("Unsupported file type. Allowed types are: " + string.Join(", ", AllowedContentTypes));
        }
        _logger.LogInformation("Uploading file: {FileName} ({ContentType}, {FileSize} bytes)", file.FileName, file.ContentType, file.Length);
        var publicId=Guid.NewGuid().ToString();
        await using var stream = file.OpenReadStream();
        if (file.ContentType.StartsWith("image/"))
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicId
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {ErrorMessage}", result.Error.Message);
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            }
            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            }; 
        }
        else if (file.ContentType.StartsWith("video/"))
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicId
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {ErrorMessage}", result.Error.Message);
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            }
            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }
        else
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicId
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {ErrorMessage}", result.Error.Message);
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            }
            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }

    }
}

