using Microsoft.AspNetCore.Http;

namespace ChatApp.Application.DTOs.Request;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = null!;
}

public class UpdateProfilePictureRequest
{
    public IFormFile ProfilePicture { get; set; } = null!;
}