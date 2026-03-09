using ChatApp.Application.DTOs.Request;
using ChatApp.Application.DTOs.Response;
using ChatApp.Application.DTOs.WebSocket;
using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationRepository _conversationRepo;
    private readonly ICloudinaryService _cloudiaryService;
    private readonly ILogger<MessageService> _logger;
    public MessageService(IMessageRepository messageRepo, ICloudinaryService cloudinaryService ,IConversationRepository _conversationRepository, ILogger<MessageService> logger)
    {
        _messageRepo = messageRepo;
        _cloudiaryService = cloudinaryService;
        _conversationRepo = _conversationRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<MessageResponse>> SaveMessageAsync(
    Guid senderId, SendMessagePayload payload)
    {
        try
        {
            _logger.LogInformation("Saving message from UserId={SenderId} to ConversationId={ConvId}",
                senderId, payload.ConversationId);

            var participantIds = await _messageRepo
                .GetConversationParticipantIdsAsync(payload.ConversationId);

            if (!participantIds.Contains(senderId))
            {
                _logger.LogWarning("UserId={UserId} is not a participant of ConversationId={ConvId}",
                    senderId, payload.ConversationId);
                return ApiResponse<MessageResponse>.Fail(
                    "You are not a participant of this conversation.", 403, nameof(SaveMessageAsync));
            }

            await _messageRepo.MarkAllAsReadAsync(payload.ConversationId, senderId);
            _logger.LogInformation("Marked all previous messages as read for sender UserId={UserId}", senderId);

            var message = new Message
            {
                ConversationId = payload.ConversationId,
                SenderId = senderId,
                Content = payload.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var participantId in participantIds)
            {
                message.Reciepts.Add(new MessageReciept
                {
                    UserId = participantId,
                    Status = participantId == senderId
                        ? MessageStatus.Read
                        : MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var saved = await _messageRepo.CreateMessageAsync(message);

            var conversation = await _conversationRepo.GetByIdAsync(
                payload.ConversationId, senderId);

            if (conversation is not null)
            {
                conversation.LastMessageId = saved.Id;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversationRepo.UpdateLastMessageAsync(conversation);
            }

            _logger.LogInformation("Message saved with Id={MessageId}", saved.Id);
            return ApiResponse<MessageResponse>.Ok(MapToResponse(saved, senderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(SaveMessageAsync));
            return ApiResponse<MessageResponse>.Fail(
                "Something went wrong.", 500, nameof(SaveMessageAsync));
        }
    }

    public async Task<ApiResponse<MessageResponse>> SendFileMessageAsync(Guid senderId, Guid conversationId, SendFileMessageRequest request)
    {
        try
        {
            _logger.LogInformation("Saving file message from UserId={SenderId} to ConversationId={ConvId}",
                senderId,conversationId);

            var participantIds = await _messageRepo
                .GetConversationParticipantIdsAsync(conversationId);
            if (!participantIds.Contains(senderId))
            {
                return ApiResponse<MessageResponse>.Fail(
                    "You are not a participant of this conversation.", 403, nameof(SendFileMessageAsync));
            }
            CloudinaryUploadResult uploadResult;
            try
            {
                uploadResult= await _cloudiaryService.UploadFileAsync(request.File);
                _logger.LogInformation("File uploaded successfully for UserId={UserId} in ConversationId={ConvId}",
                    senderId, conversationId);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "File upload failed for UserId={UserId} in ConversationId={ConvId}",
                    senderId, conversationId);
                return ApiResponse<MessageResponse>.Fail(
                    "File upload failed. Please try again.", 500, nameof(SendFileMessageAsync));
            }
            await _messageRepo.MarkAllAsReadAsync(conversationId, senderId);
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = request.Content?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<FileAttachment>
                {
                    new FileAttachment
                    {
                        FileUrl = uploadResult.Url,
                        PublicId = uploadResult.PublicId,
                        FileName = uploadResult.FileName,
                        FileSize = uploadResult.FileSize,
                        ContentType = uploadResult.ContentType
                    }
                }
            };
            foreach(var participantId in participantIds)
            {
                message.Reciepts.Add(new MessageReciept
                {
                    UserId = participantId,
                    Status = participantId == senderId
                        ? MessageStatus.Read
                        : MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                });
            }
            var saved = await _messageRepo.CreateMessageAsync(message);
            var conversation = await _conversationRepo.GetByIdAsync(
                    conversationId, senderId);
            if(conversation is not null)
            {
                conversation.LastMessageId = saved.Id;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversationRepo.UpdateLastMessageAsync(conversation);
            }
            _logger.LogInformation("File message saved with Id={MessageId} for UserId={UserId} in ConversationId={ConvId}",
                saved.Id, senderId, conversationId);
            return ApiResponse<MessageResponse>.Ok(MapToResponse(saved, senderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(SendFileMessageAsync));
            return ApiResponse<MessageResponse>.Fail(
                "Something went wrong.", 500, nameof(SendFileMessageAsync));
        }
    }
    public async Task<ApiResponse<PagedResponse<MessageResponse>>> GetMessagesAsync(Guid conversationId, Guid currentUserId, GetMessagesRequest request)
    {
        try
        {
            _logger.LogInformation("Fetching messages for ConversationId={ConvId}", conversationId);

            var participantIds = await _messageRepo
                .GetConversationParticipantIdsAsync(conversationId);

            if (!participantIds.Contains(currentUserId))
                return ApiResponse<PagedResponse<MessageResponse>>.Fail(
                    "You are not a participant of this conversation.", 403, nameof(GetMessagesAsync));

            request.PageSize = Math.Clamp(request.PageSize, 1, 50);

            await _messageRepo.MarkAllAsReadAsync(conversationId, currentUserId);
            _logger.LogInformation("Marked all messages as read for UserId={UserId} in ConversationId={ConvId}",
                currentUserId, conversationId);

            var messages = await _messageRepo.GetConversationMessagesAsync(
                conversationId, request.Page, request.PageSize);

            var totalCount = await _messageRepo.GetTotalMessagesCountAsync(conversationId);

            var result = new PagedResponse<MessageResponse>
            {
                Items = messages.Select(m => MapToResponse(m, currentUserId)).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return ApiResponse<PagedResponse<MessageResponse>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(GetMessagesAsync));
            return ApiResponse<PagedResponse<MessageResponse>>.Fail(
                "Something went wrong.", 500, nameof(GetMessagesAsync));
        }
    }

    public async Task<ApiResponse<bool>> MarkMessageAsReadAsync(Guid userId, Guid messageId)
    {
        try
        {
            _logger.LogInformation("Marking MessageId={MessageId} as read for UserId={UserId}",
                messageId, userId);

            await _messageRepo.UpdateMessageStatusAsync(messageId, userId, MessageStatus.Read);
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(MarkMessageAsReadAsync));
            return ApiResponse<bool>.Fail("Something went wrong.", 500, nameof(MarkMessageAsReadAsync));
        }
    }

    public async Task<List<Guid>> GetConversationParticipantIdsAsync(Guid conversationId)
    {
        return await _messageRepo.GetConversationParticipantIdsAsync(conversationId);
    }

    public async Task<List<MessageResponse>> GetPendingMessagesAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting pending messages for UserId={UserId}", userId);
            var messages = await _messageRepo.GetPendingMessagesAsync(userId);
            return messages.Select(m => MapToResponse(m, userId)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(GetPendingMessagesAsync));
            return new List<MessageResponse>();
        }
    }

    public async Task MarkMessageAsDeliveredAsync(Guid userId, Guid messageId)
    {
        try
        {
            _logger.LogInformation("Marking MessageId={MessageId} as delivered for UserId={UserId}",
                messageId, userId);
            await _messageRepo.UpdateMessageStatusAsync(messageId, userId, MessageStatus.Delivered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {Method}", nameof(MarkMessageAsDeliveredAsync));
        }
    }

    private static MessageResponse MapToResponse(Message message, Guid currentUserId)
    {
        // Get current user's receipt status
        var myReceipt = message.Reciepts.FirstOrDefault(r => r.UserId == currentUserId);

        
        var highestStatus = message.Reciepts
            .Where(r => r.UserId != message.SenderId)
            .OrderByDescending(r => r.Status)
            .FirstOrDefault();

        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender.FullName,
            SenderProfilePicture = message.Sender.ProfilePictureUrl,
            Content = message.Content,
            IsSystemMessage = message.IsSystemMessage,
            Status = (highestStatus?.Status ?? MessageStatus.Sent).ToString(),
            Attachments = message.Attachments.Select(a => new FileAttachmentResponse
            {
                Id = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                ContentType = a.ContentType
            }).ToList(),
        };

    }
}