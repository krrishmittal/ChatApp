using ChatApp.Application.DTOs.Request;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;

    public UserController(IUserService userService, UserManager<User> userManager)
    {
        _userService = userService;
        _userManager = userManager;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] SearchUsersRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _userService.SearchUsersAsync(userId, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var response = await _userService.GetProfileAsync(userId);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var response = await _userService.UpdateProfileAsync(userId, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("profile-picture")]
    public async Task<IActionResult> UpdateProfilePicture([FromForm] UpdateProfilePictureRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var response = await _userService.UpdateProfilePictureAsync(userId, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var response = await _userService.DeleteAccountAsync(userId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("update-fcm-token")]
    [Authorize]
    public async Task<IActionResult> UpdateFcmToken([FromBody] SaveFcmTokenRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        user.FcmToken = request.FcmToken;
        await _userManager.UpdateAsync(user);
         
        return Ok(new { success = true, message = "FCM token updated" });
    }


}