using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly ICaptchaService _captchaService;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        ICloudinaryService cloudinaryService,
        IEmailService emailService,
        ILogger<AuthService> logger,
        ICaptchaService captchaService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _cloudinaryService = cloudinaryService;
        _emailService = emailService;
        _logger = logger;
        _captchaService = captchaService;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Starting user registration for email: {Email}", request.Email);

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                _logger.LogWarning("Registration failed: Email {Email} is already in use", request.Email);
                return ApiResponse<AuthResponse>.Fail("Email is already in use.", 409, nameof(RegisterAsync));
            }

            string? profilePictureUrl = null;
            if (request.ProfilePicture is not null && request.ProfilePicture.Length > 0)
            {
                _logger.LogInformation("Uploading profile picture for {Email}", request.Email);
                profilePictureUrl = await _cloudinaryService.UploadImageAsync(request.ProfilePicture);
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                ProfilePictureUrl = profilePictureUrl,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User creation failed for {Email}: {Errors}", request.Email, errors);
                return ApiResponse<AuthResponse>.Fail(
                    result.Errors.First().Description, 400, nameof(RegisterAsync));
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });
            await _userManager.UpdateAsync(user);



            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }, "Registration successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(RegisterAsync));
            return ApiResponse<AuthResponse>.Fail("Something went wrong.", 500, nameof(RegisterAsync));
        }
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting login for email: {Email}", request.Email);

            //captcha validation
            var captchaValid = await _captchaService.ValidateAsync(request.CaptchaToken);
            if (!captchaValid)
            {
                _logger.LogWarning("Login failed: invalid captcha for {Email}", request.Email);
                return ApiResponse<AuthResponse>.Fail("Invalid captcha. Please try again.", 400, nameof(LoginAsync));
            }

            //validate user credentials
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || user.IsDeleted)
            {
                _logger.LogWarning("Login failed: user not found for {Email}", request.Email);
                return ApiResponse<AuthResponse>.Fail("Invalid email or password.", 401, nameof(LoginAsync));
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Login failed: wrong password for {Email}", request.Email);
                return ApiResponse<AuthResponse>.Fail("Invalid email or password.", 401, nameof(LoginAsync));
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            foreach (var token in user.RefreshTokens.Where(t => !t.IsRevoked))
                token.IsRevoked = true;

            user.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });

            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Login successful for email: {Email}", request.Email);

            await _emailService.SendLoginNotificationEmailAsync(request.Email, user.FullName);

            return ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(LoginAsync));
            return ApiResponse<AuthResponse>.Fail("Something went wrong.", 500, nameof(LoginAsync));
        }
    }


    public async Task<ApiResponse<bool>>ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            _logger.LogInformation("Processing forgot password for email: {Email}", request.Email);

            var user=await _userManager.FindByEmailAsync(request.Email);

            if (user is not null && !user.IsDeleted) {
                var otp = new Random().Next(100000, 999999).ToString();
                user.PasswordResetOtp = otp;
                user.PassordResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);
                await _userManager.UpdateAsync(user);

                await _emailService.SendOtpEmailAsync(request.Email, user.FullName, otp);
                _logger.LogInformation("Forgot password: OTP sent to email {Email}", request.Email);
            }

            return ApiResponse<bool>.Ok(true, "If email exists, an OTP has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(ForgotPasswordAsync));
            return ApiResponse<bool>.Fail("Something went wrong.", 500, nameof(ForgotPasswordAsync));
        }
    }
    
    public async Task<ApiResponse<bool>>ResetPasswordAsync(ResetPasswordRequest request)
    {

        try
        {
            _logger.LogInformation("Processing password reset for email: {Email}", request.Email);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if(user is not null && !user.IsDeleted)
            {
                if (user.PasswordResetOtp != request.OtpCode || user.PassordResetOtpExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning("Password reset failed: invalid or expired OTP for {Email}", request.Email);
                    return ApiResponse<bool>.Fail("Invalid or expired OTP.", 400, nameof(ResetPasswordAsync));
                }
                if (request.NewPassword != request.ConfirmPassword)
                {
                    _logger.LogWarning("Password reset failed: password mismatch for {Email}", request.Email);
                    return ApiResponse<bool>.Fail("Passwords do not match.", 400, nameof(ResetPasswordAsync));
                }
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password reset failed for {Email}: {Errors}", request.Email, errors);
                    return ApiResponse<bool>.Fail(
                        result.Errors.First().Description, 400, nameof(ResetPasswordAsync));
                }
                user.PasswordResetOtp = null;
                user.PassordResetOtpExpiry = null;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Password reset successful for email: {Email}", request.Email);
            }

            return ApiResponse<bool>.Ok(true, "Password reset successful.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(ResetPasswordAsync));
            return ApiResponse<bool>.Fail("Something went wrong.", 500, nameof(ResetPasswordAsync));
        }
    }

    

}
