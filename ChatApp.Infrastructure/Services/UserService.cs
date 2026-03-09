using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<UserService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IUserRepository _userRepo;

    public UserService(
        UserManager<User> userManager, ICloudinaryService cloudinaryService, ConnectionManager connectionManager,ILogger<UserService> logger, AppDbContext dbContext, IUserRepository userRepo)
    {
        _userManager = userManager;
        _cloudinaryService = cloudinaryService;
        _connectionManager = connectionManager;
        _logger = logger;
        _dbContext = dbContext;
        _userRepo = userRepo;

    }

    public async Task<ApiResponse<PagedResponse<UserResponse>>> SearchUsersAsync(Guid currentUserId, SearchUsersRequest request)
    {
        try
        {
            _logger.LogInformation("Searching users with query: {SearchQuery}", request.Search);
            request.Page = Math.Clamp(request.Page, 1, 50);
            var (users, totalCount) = await _userRepo.SearchUsersAsync(currentUserId, request);
            var result = new PagedResponse<UserResponse>
            {
                Items = users.Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    IsOnline = _connectionManager.IsOnline(u.Id)
                }).ToList(),
            };
            return ApiResponse<PagedResponse<UserResponse>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(SearchUsersAsync));
            return ApiResponse<PagedResponse<UserResponse>>.Fail("Something went wrong.", 500, nameof(SearchUsersAsync));
        }
    }

    public async Task<ApiResponse<UserProfileResponse>> GetProfileAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                _logger.LogWarning("Profile not found for user ID: {UserId}", userId);
                return ApiResponse<UserProfileResponse>.Fail("User not found", 404, nameof(GetProfileAsync));
            }
            return ApiResponse<UserProfileResponse>.Ok(MapToProfileResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(GetProfileAsync));
            return ApiResponse<UserProfileResponse>.Fail("Something went wrong.", 500, nameof(GetProfileAsync));
        }
    }

    public async Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Updating profile for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                _logger.LogWarning("Update Failed: user not found for ID {UserId}", userId);
                return ApiResponse<UserProfileResponse>.Fail("User not found", 404, nameof(UpdateProfileAsync));
            }
            user.FullName = request.FullName;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Profile update failed for user ID {UserId}:{Errors}", userId, string.Join(",", result.Errors.Select(e => e.Description)));
                return ApiResponse<UserProfileResponse>.Fail(result.Errors.First().Description, 400, nameof(UpdateProfileAsync));
            }
            _logger.LogInformation("Profile updated for user ID: {UserId}", userId);
            return ApiResponse<UserProfileResponse>.Ok(MapToProfileResponse(user), "Profile Updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error in {Method}", nameof(UpdateProfileAsync));
            return ApiResponse<UserProfileResponse>.Fail("Something went wrong", 500, nameof(UpdateProfileAsync));
        }
    }

    public async Task<ApiResponse<UserProfileResponse>> UpdateProfilePictureAsync(string userId, UpdateProfilePictureRequest request)
    {
        try
        {
            _logger.LogInformation("Updating profile picture for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                _logger.LogWarning("Update picture failed: user not found for ID {UserId}", userId);
                return ApiResponse<UserProfileResponse>.Fail("User not found.", 404, nameof(UpdateProfilePictureAsync));
            }

            // Delete old picture from Cloudinary if it exists and is not a Google profile picture
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.Contains("cloudinary"))
            {
                var publicId = ExtractPublicId(user.ProfilePictureUrl);
                await _cloudinaryService.DeleteImageAsync(publicId);
            }

            var newUrl = await _cloudinaryService.UploadImageAsync(request.ProfilePicture);
            user.ProfilePictureUrl = newUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Profile picture updated for user ID: {UserId}", userId);
            return ApiResponse<UserProfileResponse>.Ok(MapToProfileResponse(user), "Profile picture updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error in {Method}", nameof(UpdateProfilePictureAsync));
            return ApiResponse<UserProfileResponse>.Fail("Something went wrong", 500, nameof(UpdateProfilePictureAsync));
        }
    }

    public async Task<ApiResponse<bool>> DeleteAccountAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting account for user ID: {UserId}", userId);
            var userGuid = Guid.Parse(userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("Delete Failed: user not found for ID {UserId}", userId);
                return ApiResponse<bool>.Fail("User not found", 404, nameof(DeleteAccountAsync));
            }

            // 1. Delete profile picture from Cloudinary
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.Contains("cloudinary"))
            {
                var publicId = ExtractPublicId(user.ProfilePictureUrl);
                await _cloudinaryService.DeleteImageAsync(publicId);
            }

            // 2. Delete Cloudinary file attachments from user's messages
            var attachmentUrls = await _dbContext.FileAttachments
                .IgnoreQueryFilters()
                .Where(fa => fa.Message.SenderId == userGuid)
                .Select(fa => fa.FileUrl)
                .ToListAsync();

            foreach (var url in attachmentUrls.Where(u => u.Contains("cloudinary")))
            {
                var publicId = ExtractPublicId(url);
                await _cloudinaryService.DeleteImageAsync(publicId);
            }

            // 3. Remove message receipts for this user (NoAction FK — not cascade-deleted)
            var userReceipts = await _dbContext.MessageReciepts
                .IgnoreQueryFilters()
                .Where(mr => mr.UserId == userGuid)
                .ToListAsync();
            _dbContext.MessageReciepts.RemoveRange(userReceipts);

            // 4. Remove messages sent by this user (NoAction FK — not cascade-deleted)
            //    FileAttachments and MessageReceipts on these messages are cascade-deleted automatically
            var userMessages = await _dbContext.Messages
                .IgnoreQueryFilters()
                .Where(m => m.SenderId == userGuid)
                .ToListAsync();
            _dbContext.Messages.RemoveRange(userMessages);

            // 5. Save manual deletions before removing the user
            await _dbContext.SaveChangesAsync();

            // 6. Delete user (cascades RefreshTokens and ConversationParticipants)
            await _userManager.DeleteAsync(user);

            _logger.LogInformation("All user data deleted for user ID: {UserId}", userId);
            return ApiResponse<bool>.Ok(true, "User account deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error in {Method}", nameof(DeleteAccountAsync));
            return ApiResponse<bool>.Fail("Something went wrong", 500, nameof(DeleteAccountAsync));
        }
    }

    public async Task<ApiResponse<bool>> SaveFcmTokenAsync(string userId, string fcmToken)
    {
        try
        {
            _logger.LogInformation("Saving FCM token for user ID: {UserId}", userId);
            var user =await _userManager.FindByIdAsync(userId);
            if(user == null || user.IsDeleted) {
                _logger.LogWarning("Save FCM token failed: user not found for ID {UserId}", userId);
                return ApiResponse<bool>.Fail("User not found", 404, nameof(SaveFcmTokenAsync));
            }
            user.FcmToken=fcmToken;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("FCM token saved for user ID: {UserId}", userId);
            return ApiResponse<bool>.Ok(true, "FCM token saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected Error in {Method}", nameof(SaveFcmTokenAsync));
            return ApiResponse<bool>.Fail("Something went wrong", 500, nameof(SaveFcmTokenAsync));
        }
    }
    private static UserProfileResponse MapToProfileResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email!,
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsGoogleAccount = user.IsGoogleAccount,
        CreatedAt = user.CreatedAt
    };

    private static string ExtractPublicId(string cloudinaryUrl)
    {
        // URL format: https://res.cloudinary.com/{cloud}/image/upload/v123/chat-app/{publicId}.jpg
        var uri = new Uri(cloudinaryUrl);
        var segments = uri.AbsolutePath.Split('/');
        var fileName = segments[^1]; // last segment
        var folder = segments[^2];   // "chat-app"
        var publicIdWithExtension = $"{folder}/{fileName}";
        return Path.GetFileNameWithoutExtension(publicIdWithExtension).Insert(0, $"{folder}/");
    }
}