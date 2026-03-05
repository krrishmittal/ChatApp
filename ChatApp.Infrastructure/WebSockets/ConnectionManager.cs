using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ChatApp.Infrastructure.WebSockets;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections = new();
    private readonly ILogger<ConnectionManager> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _logger = logger;
    }

    public void AddConnection(Guid userId, WebSocket socket)
    {
        _connections[userId] = socket;
        _logger.LogInformation("Added connection for user {UserId}", userId);
    }

    public void RemoveConnection(Guid userId)
    {
        _connections.TryRemove(userId, out _);
        _logger.LogInformation("Removed connection for user {UserId}", userId);
    }

    public bool isOnline(Guid userId)
    {
        return _connections.ContainsKey(userId);
    }
    public IEnumerable<Guid> GetOnlineUserIds() => _connections.Keys;

    public async Task SendToUserAsync(Guid userId, object message)
    {
        if (!_connections.TryGetValue(userId, out var socket))
        {
            return;
        }
        if (socket.State != WebSocketState.Open)
        {
            return;
        }
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to user {UserId}", userId);
            RemoveConnection(userId);
        }
    }
    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, object message)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, message));
        await Task.WhenAll(tasks);
    }
}