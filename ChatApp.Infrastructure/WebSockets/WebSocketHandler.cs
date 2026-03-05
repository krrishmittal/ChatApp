using ChatApp.Application.DTOs.WebSocket;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
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

    public async Task HandleAsync(Guid userId, WebSocket socket)
    {
        _connectionManager.AddConnection(userId, socket);

        await UpdateUserStatusAsync(userId, UserStatus.Online);
        await BroadcastUserStatusAsync(userId, isOnline: true);

        var buffer = new byte[4096];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Close requested by UserId={UserId}", userId);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Message received from UserId={UserId}: {Raw}",
                        userId, raw);
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
            _connectionManager.RemoveConnection(userId);
            await UpdateUserStatusAsync(userId, UserStatus.Offline);
            await BroadcastUserStatusAsync(userId, isOnline: false);

            if (socket.State != WebSocketState.Closed)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }
        }
    }

    private async Task UpdateUserStatusAsync(Guid userId, UserStatus status)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                user.Status = status;
                await userManager.UpdateAsync(user);
                _logger.LogInformation("User {UserId} status updated to {Status}", userId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for UserId={UserId}", userId);
        }
    }

    private async Task RouteMessageAsync(Guid userId, string raw)
    {
        try
        {
            var message = JsonSerializer.Deserialize<WebSocketMessage>(raw, _jsonOptions);
            if (message is null)
            {
                await SendErrorAsync(userId, "Invalid message format.");
                return;
            }

            switch (message.Type)
            {
                case WebSocketMessageTypes.Ping:
                    await HandlePingAsync(userId);
                    break;

                case WebSocketMessageTypes.SendMessage:
                    var sendPayload = DeserializePayload<SendMessagePayload>(message.Payload);
                    if (sendPayload is not null)
                        await HandleSendMessageAsync(userId, sendPayload);
                    break;

                case WebSocketMessageTypes.Typing:
                    var typingPayload = DeserializePayload<TypingPayload>(message.Payload);
                    if (typingPayload is not null)
                        await HandleTypingAsync(userId, typingPayload);
                    break;

                case WebSocketMessageTypes.ReadReceipt:
                    var readPayload = DeserializePayload<ReadReceiptPayload>(message.Payload);
                    if (readPayload is not null)
                        await HandleReadReceiptAsync(userId, readPayload);
                    break;

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

    // ─── Handlers ────────────────────────────────────────────────

    private async Task HandlePingAsync(Guid userId)
    {
        _logger.LogInformation("Ping from UserId={UserId}", userId);
        await _connectionManager.SendToUserAsync(userId, new
        {
            type = WebSocketMessageTypes.Pong,
            payload = new { timestamp = DateTime.UtcNow }
        });
    }

    private async Task HandleSendMessageAsync(Guid userId, SendMessagePayload payload)
    {
        _logger.LogInformation("SendMessage from UserId={UserId} to ConversationId={ConvId}",
            userId, payload.ConversationId);

        await Task.CompletedTask;
    }

    private async Task HandleTypingAsync(Guid userId, TypingPayload payload)
    {
        _logger.LogInformation("Typing indicator from UserId={UserId} ConversationId={ConvId} IsTyping={IsTyping}",
            userId, payload.ConversationId, payload.IsTyping);

        await Task.CompletedTask;
    }

    private async Task HandleReadReceiptAsync(Guid userId, ReadReceiptPayload payload)
    {
        _logger.LogInformation("ReadReceipt from UserId={UserId} MessageId={MsgId}",
            userId, payload.MessageId);

        await Task.CompletedTask;
    }

    // ─── Helpers ─────────────────────────────────────────────────

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

    private static T? DeserializePayload<T>(object? payload)
    {
        if (payload is null) return default;
        var json = JsonSerializer.Serialize(payload);
        return JsonSerializer.Deserialize<T>(json);
    }
}