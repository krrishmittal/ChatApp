using ChatApp.Application.DTOs.Response;
using ChatApp.Application.DTOs.WebSocket;
using ChatApp.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.WebSockets;

public class WebSocketNotifier
{
    private readonly ConnectionManager _connectionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebSocketNotifier> _logger;

    public WebSocketNotifier(
        ConnectionManager connectionManager,
        IServiceProvider serviceProvider,
        ILogger<WebSocketNotifier> logger)
    {
        _connectionManager = connectionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // notify all the participants in the conversation about the new messages other than the sender and confirm to the sender that the message was sent successfully
    public async Task NotifyNewMessageAsync(MessageResponse message, Guid senderId)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageService = scope.ServiceProvider
            .GetRequiredService<IMessageService>();

        var participantIds = await messageService
            .GetConversationParticipantIdsAsync(message.ConversationId);

        foreach (var participantId in participantIds)
        {
            var isOnline = _connectionManager.IsOnline(participantId);

            if (isOnline && participantId != senderId)
            {
                await _connectionManager.SendToUserAsync(participantId, new
                {
                    type = WebSocketMessageTypes.NewMessage,
                    payload = message
                });

                await messageService.MarkMessageAsDeliveredAsync(
                    participantId, message.Id);

                _logger.LogInformation(
                    "File message {MessageId} delivered to UserId={UserId}",
                    message.Id, participantId);
            }
        }

        // Confirm to sender
        await _connectionManager.SendToUserAsync(senderId, new
        {
            type = WebSocketMessageTypes.MessageSent,
            payload = message
        });
    }
}