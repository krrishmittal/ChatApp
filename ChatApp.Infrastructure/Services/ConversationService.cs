using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Identity;
using ChatApp.Domain.Entities;
using Microsoft.Extensions.Logging;
namespace ChatApp.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepo;
    private readonly ConnectionManager _connectionManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(IConversationRepository conversationRepo,
        ConnectionManager connectionManager,
        UserManager<User> userManager,
        ILogger<ConversationService> logger)
    {
        _conversationRepo = conversationRepo;
        _connectionManager = connectionManager;
        _userManager = userManager;
        _logger = logger;
    }
    public async Task<ApiResponse<ConversationResponse>> CreateOrGetConversationAsync(Guid currentUserId, CreateConversationRequest request)
    {
        try
        {
            _logger.LogInformation("CreateOrGet conversation between {UserA} and {UserB}",
                currentUserId, request.TargetUserId);
            if (currentUserId == request.TargetUserId)
            {
                return ApiResponse<ConversationResponse>.Fail("Cannot create conversation with yourself", 400, "CreateOrGetConversationAsync");
            }
            var targetUser = await _userManager.FindByIdAsync(request.TargetUserId.ToString());
            if (targetUser == null)
            {
                return ApiResponse<ConversationResponse>.Fail("Target user not found", 404, "CreateOrGetConversationAsync");
            }
            var existing = await _conversationRepo.GetExistingConvesationAsync(currentUserId, request.TargetUserId);
            if (existing is not null)
            {
                _logger.LogInformation("Existing conversation found with id {ConversationId}", existing.Id);
                return ApiResponse<ConversationResponse>.Ok(MapToResponse(existing, currentUserId), "Conversation already exists");
            }
            var conversation = await _conversationRepo.CreateConversationAsync(currentUserId, request.TargetUserId);
            _logger.LogInformation("New conversation created with id {ConversationId}", conversation.Id);
            return ApiResponse<ConversationResponse>.Ok(MapToResponse(conversation, currentUserId), "Conversation created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateOrGetConversationAsync for users {UserA} and {UserB}", currentUserId, request.TargetUserId);
            return ApiResponse<ConversationResponse>.Fail("An error occurred while creating conversation", 500, "CreateOrGetConversationAsync");
        }
    }
    public async Task<ApiResponse<List<ConversationResponse>>> GetMyConversationsAsync(Guid currentUserId)
    {
        try
        {
            _logger.LogInformation("Fetching conversations for UserId={UserId}", currentUserId);

            var conversations = await _conversationRepo.GetUserConversationsAsync(currentUserId);

            var result = conversations
                .Select(c => MapToResponse(c, currentUserId))
                .ToList();

            return ApiResponse<List<ConversationResponse>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(GetMyConversationsAsync));
            return ApiResponse<List<ConversationResponse>>.Fail(
                "Something went wrong.", 500, nameof(GetMyConversationsAsync));
        }
    }

    private ConversationResponse MapToResponse(Conversation conversation, Guid currentUserId)
    {
        var otherParticipant = conversation.Participants.First(p => p.UserId != currentUserId);
        return new ConversationResponse
        {
            Id = conversation.Id,
            OtherUser = new UserResponse
            {
                Id = otherParticipant.User.Id,
                FullName = otherParticipant.User.FullName,
                Email = otherParticipant.User.Email,
                ProfilePictureUrl = otherParticipant.User.ProfilePictureUrl,
                IsOnline = _connectionManager.IsOnline(otherParticipant.UserId)
            },
            LastMessage = conversation.LastMessage?.Content,
            LastMessageAt = conversation.LastMessage?.CreatedAt,
            UnreadCount = 0,
            CreatedAt = conversation.CreatedAt
        };
    }
}