using Microsoft.AspNetCore.Http;

namespace ChatApp.Application.DTOs.Request;
public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public IFormFile? ProfilePicture { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string CaptchaToken { get; set; } = null!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}
public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}