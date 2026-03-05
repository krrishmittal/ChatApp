namespace ChatApp.Application.DTOs.WebSocket;
public class WebSocketMessage
{
    public string Type { get; set; } = null!;

    public object? Payload { get; set; }
}
