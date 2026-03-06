using System.Text.Json;

namespace ChatApp.Application.DTOs.WebSocket;

public class WebSocketMessage
{
    public string Type { get; set; } = null!;
    public JsonElement? Payload { get; set; }
}