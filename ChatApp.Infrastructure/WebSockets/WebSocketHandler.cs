using ChatApp.Application.DTOs.WebSocket;
using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChatApp.Infrastructure.WebSockets;

public class WebSocketHandler
{
    private readonly ConnectionManager _connectionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebSocketHandler> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public WebSocketHandler(
        ConnectionManager connectionManager,ILogger<WebSocketHandler> logger,IServiceProvider serviceProvider)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // main entry point for handling the websocket connection and then route the message to their respective handlers and then save the message in the database and then send the message to the other participant 
    public async Task HandleAsync(Guid userId, WebSocket socket)
    {
        // adding connection to the connection manager and then broadcast the user status online to other users and then deliever the pending messages to user if there are any left undelivered messages for the user
        _connectionManager.AddConnection(userId, socket);
        await BroadcastUserStatusAsync(userId, isOnline: true);
        await DeliverPendingMessagesAsync(userId);

        //buffer for receiving messages from the client
        var buffer = new byte[4096];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                //waiting for the message from the client and then route the message to their respective handler
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                // if the message type is close then break the loop and then remove the connection
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Close requested by UserId={UserId}", userId);
                    break;
                }

                // if the message type is text then route the message to their respective handler
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Message received from UserId={UserId}: {Raw}",
                        userId, raw);
                    //routing
                    await RouteMessageAsync(userId, raw);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error for UserId={UserId}", userId);
        }
        finally
        {
            _connectionManager.RemoveConnection(userId, socket);
            
            if (!_connectionManager.IsOnline(userId))
            {
                await BroadcastUserStatusAsync(userId, isOnline: false);
            }

            if (socket.State != WebSocketState.Closed)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }
        }
    }

    //function for handling the message recieved from the payload and then save the message in the database and then send the message to the other participant if they are online otherwise send push notification to the other participant
    private async Task HandleSendMessageAsync(Guid userId, SendMessagePayload payload)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

            // calling the save message function in the message service and then get the result of the save message function
            var result = await messageService.SaveMessageAsync(userId, payload);

            _logger.LogInformation("SaveMessage result: Success={Success} Message={Msg}",
                result.Success, result.Message);

            if (!result.Success)
            {
                await SendErrorAsync(userId, result.Message);
                return;
            }
            // if message saved successfully then get the participant ids of the conversation and then send the message to the other participant
            var participantIds = await messageService
                .GetConversationParticipantIdsAsync(payload.ConversationId);

            _logger.LogInformation("Participants count: {Count}", participantIds.Count);

            foreach (var participantId in participantIds)
            {
                var isOnline = _connectionManager.IsOnline(participantId);
                _logger.LogInformation("Participant {Id} IsOnline={Online}", participantId, isOnline);

                if (isOnline)
                {
                    if (participantId != userId)
                    {
                        // send the message to the other participant if they are online
                        await _connectionManager.SendToUserAsync(participantId, new
                        {
                            type = WebSocketMessageTypes.NewMessage,
                            payload = result.Data
                        });

                        _logger.LogInformation("Marking MessageId={MessageId} as delivered for UserId={UserId}",
                            result.Data!.Id, participantId);

                        // mark the message as delivered in the database for the recipient
                        await messageService.MarkMessageAsDeliveredAsync(
                            participantId, result.Data!.Id);
                        
                        result.Data.Status = "Delivered";
                    }
                }
                else
                {
                    _logger.LogInformation("Participant {Id} is offline - sending push notification", participantId);

                    using var pushScope = _serviceProvider.CreateScope();
                    var userManager = pushScope.ServiceProvider
                        .GetRequiredService<UserManager<User>>();
                    var pushService = pushScope.ServiceProvider
                        .GetRequiredService<IPushNotificationService>();

                    var recipient = await userManager.FindByIdAsync(participantId.ToString());

                    if (recipient?.FcmToken is not null)
                    {
                        await pushService.SendPushNotificationAsync(
                            recipient.FcmToken,
                            result.Data!.SenderName,
                            result.Data!.Content,
                            result.Data!.ConversationId);
                    }
                }
            }
            // finally send the message sent acknowledgement to the sender
            await _connectionManager.SendToUserAsync(userId, new
            {
                type = WebSocketMessageTypes.MessageSent,  
                payload = result.Data             
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HandleSendMessageAsync for UserId={UserId}", userId);
            await SendErrorAsync(userId, "Failed to send message.");
        }
    }

    // function for routing the message to their respective handler based on the message type and then save the message in the database
    private async Task RouteMessageAsync(Guid userId, string raw)
    {
        try
        {
            // deserialize the message from the client and then route the message to their respective handler based on the message type
            var message = JsonSerializer.Deserialize<WebSocketMessage>(raw, _jsonOptions);
            if (message is null)
            {
                await SendErrorAsync(userId, "Invalid message format.");
                return;
            }

            switch (message.Type)
            {
                // case for sending the message to server and then server will call the handesend message function and then save message in the message service 
                case WebSocketMessageTypes.SendMessage:
                    var sendPayload = DeserializePayload<SendMessagePayload>(message.Payload);
                    if (sendPayload is not null)
                        await HandleSendMessageAsync(userId, sendPayload);
                    break;

                // case for sending the typing indicator to the other user 
                case WebSocketMessageTypes.Typing:
                    var typingPayload = DeserializePayload<TypingPayload>(message.Payload);
                    if (typingPayload is not null)
                        await HandleTypingAsync(userId, typingPayload);
                    break;

                // case for marking the message as read and then notify the other participant that our message has been read by the user
                case WebSocketMessageTypes.ReadReceipt:
                    var readPayload = DeserializePayload<ReadReceiptPayload>(message.Payload);
                    if (readPayload is not null)
                        await HandleReadReceiptAsync(userId, readPayload);
                    break;

                // case for unknown message type
                default:
                    _logger.LogWarning("Unknown message type: {Type} from UserId={UserId}",
                        message.Type, userId);
                    await SendErrorAsync(userId, $"Unknown message type: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message from UserId={UserId}", userId);
            await SendErrorAsync(userId, "Failed to process message.");
        }
    }

    // function for handling the typing indicator and then send the typing indicator to the other participant if they are online
    private async Task HandleTypingAsync(Guid userId, TypingPayload payload)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

        var participantIds = await messageService
            .GetConversationParticipantIdsAsync(payload.ConversationId);

        var others = participantIds.Where(id => id != userId);

        await _connectionManager.SendToUsersAsync(others, new
        {
            type = WebSocketMessageTypes.TypingIndicator,
            payload = new
            {
                conversationId = payload.ConversationId,
                userId,
                isTyping = payload.IsTyping
            }
        });

        _logger.LogInformation("Typing indicator sent from UserId={UserId} IsTyping={IsTyping}",
            userId, payload.IsTyping);
    }

    //function for marking message as read and notify other participant that our message has been read by the user
    private async Task HandleReadReceiptAsync(Guid userId, ReadReceiptPayload payload)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        
        await messageService.MarkMessageAsReadAsync(userId, payload.MessageId);
        
        var participantIds = await messageService
            .GetConversationParticipantIdsAsync(payload.ConversationId);

        var others = participantIds.Where(id => id != userId);
        // 
        await _connectionManager.SendToUsersAsync(others, new
        {
            type = WebSocketMessageTypes.MessageRead,
            payload = new
            {
                messageId = payload.MessageId,
                conversationId = payload.ConversationId,
                userId,
                readAt = DateTime.UtcNow
            }
        });

        _logger.LogInformation("MessageId={MessageId} marked as read by UserId={UserId}",
            payload.MessageId, userId);
    }


    // function for delivering the pending messages to the user when they come online and then mark the message as delivered in the db and then notify then sender that therir message has been delivered to the user
    private async Task DeliverPendingMessagesAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Checking pending messages for UserId={UserId}", userId);

            using var scope = _serviceProvider.CreateScope();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

            var pendingMessages = await messageService.GetPendingMessagesAsync(userId);

            _logger.LogInformation("Found {Count} pending messages for UserId={UserId}",
                pendingMessages.Count, userId);

            if (!pendingMessages.Any()) return;

            foreach (var message in pendingMessages)
            {
                await _connectionManager.SendToUserAsync(userId, new
                {
                    type = WebSocketMessageTypes.NewMessage,
                    payload = message
                });

                await messageService.MarkMessageAsDeliveredAsync(userId, message.Id);
                _logger.LogInformation("Delivered pending MessageId={MessageId} to UserId={UserId}",
                    message.Id, userId);

                // Notify the SENDER that their message was delivered
                if (_connectionManager.IsOnline(message.SenderId))
                {
                    await _connectionManager.SendToUserAsync(message.SenderId, new
                    {
                        type = WebSocketMessageTypes.MessageDelivered,
                        payload = new
                        {
                            messageId = message.Id,
                            conversationId = message.ConversationId,
                            deliveredTo = userId,
                            deliveredAt = DateTime.UtcNow
                        }
                    });
                    _logger.LogInformation("Notified sender {SenderId} that MessageId={MessageId} was delivered",
                        message.SenderId, message.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver pending messages to UserId={UserId}", userId);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────

    // function for broadcasting the user status (online/offline) to all other users when a user comes online or goes offline
    private async Task BroadcastUserStatusAsync(Guid userId, bool isOnline)
    {
        var type = isOnline
            ? WebSocketMessageTypes.UserOnline
            : WebSocketMessageTypes.UserOffline;

        var others = _connectionManager.GetOnlineUserIds()
            .Where(id => id != userId);

        await _connectionManager.SendToUsersAsync(others, new
        {
            type,
            payload = new { userId }
        });

        _logger.LogInformation("Broadcasted {Status} for UserId={UserId}",
            isOnline ? "online" : "offline", userId);
    }

    private async Task SendErrorAsync(Guid userId, string message)
    {
        await _connectionManager.SendToUserAsync(userId, new
        {
            type = WebSocketMessageTypes.Error,
            payload = new { message }
        });
    }

    // generic function for deserializing the payload of the message from the client and then return the deserialized object
    private static T? DeserializePayload<T>(System.Text.Json.JsonElement? payload)
    {
        if (payload is null) return default;
        return payload.Value.Deserialize<T>(_jsonOptions);
    }
}