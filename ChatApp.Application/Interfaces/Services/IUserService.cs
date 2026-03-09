using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;

namespace ChatApp.Application.Interfaces.Services;

public interface IUserService
{
    Task<ApiResponse<PagedResponse<UserResponse>>> SearchUsersAsync(
       Guid currentUserId, SearchUsersRequest request);

    Task<ApiResponse<UserProfileResponse>> GetProfileAsync(string userId);
    Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<ApiResponse<UserProfileResponse>> UpdateProfilePictureAsync(string userId, UpdateProfilePictureRequest request);
    Task<ApiResponse<bool>> DeleteAccountAsync(string userId);
    Task<ApiResponse<bool>> SaveFcmTokenAsync(string userId, string fcmToken);
}