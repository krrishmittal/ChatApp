using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChatApp.Infrastructure.WebSockets;

public class ConnectionManager
{
    // Map UserId to a set of WebSockets (using ConcurrentDictionary as a ConcurrentHashSet)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<WebSocket, byte>> _userSockets = new();
    private readonly ILogger<ConnectionManager> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _logger = logger;
    }

    // adds a websocket connection for a user a user can have multiple connections through different devices or browser
    public void AddConnection(Guid userId, WebSocket socket)
    {
        // get or create the set of sockets for the user
        // what is concurrentdictionary and why we use it here?
        // ConcurrentDictionary is a thread-safe collection that allows concurrent read and write operations without the need for external locking.
        var sockets = _userSockets.GetOrAdd(userId, _ => new ConcurrentDictionary<WebSocket, byte>());
        sockets.TryAdd(socket, 1);
        _logger.LogInformation("Added connection for user {UserId}. Total sockets: {Count}", userId, sockets.Count);
    }

    //removes a websocket connection for a user if the user has no nore connections we remove the user from the dictionary 
    public void RemoveConnection(Guid userId, WebSocket socket)
    {
        if (_userSockets.TryGetValue(userId, out var sockets))
        {
            sockets.TryRemove(socket, out _);
            _logger.LogInformation("Removed a connection for user {UserId}. Remaining sockets: {Count}", userId, sockets.Count);
            
            if (sockets.IsEmpty)
            {
                _userSockets.TryRemove(userId, out _);
                _logger.LogInformation("User {UserId} has no more connections and is now totally offline.", userId);
            }
        }
    }

    // this methods checks if a user is online by checking if they have any active websocket connections in the dictionary. If the user has at least one connection, they are considered online.
    public bool IsOnline(Guid userId)
    {
        return _userSockets.TryGetValue(userId, out var sockets) && !sockets.IsEmpty;
    }
    
    public IEnumerable<Guid> GetOnlineUserIds() => _userSockets.Keys;

    // this method sends a message to all active websocket connections of a specific user. It first checks if the user has any active connections then serializes the message to json and sends it to each open socket 
    public async Task SendToUserAsync(Guid userId, object message)
    {
        if (!_userSockets.TryGetValue(userId, out var sockets) || sockets.IsEmpty)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        // we use select to create a list of tasks for sending the message to each socket
        var tasks = sockets.Keys.Select(async socket =>
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to user {UserId} on socket", userId);
                    RemoveConnection(userId, socket);
                }
            }
            else
            {
                RemoveConnection(userId, socket);
            }
        });

        await Task.WhenAll(tasks);
    }
    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, object message)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, message));
        await Task.WhenAll(tasks);
    }
}