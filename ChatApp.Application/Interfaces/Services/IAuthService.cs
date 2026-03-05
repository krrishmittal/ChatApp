using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;

namespace ChatApp.Application.Interfaces.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request, string userId);
}