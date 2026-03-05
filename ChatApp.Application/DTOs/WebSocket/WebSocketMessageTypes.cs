namespace ChatApp.Application.DTOs.WebSocket;
public class WebSocketMessageTypes
{

    // client ->server
    public const string SendMessage = "send_message";
    public const string Typing= "typing";
    public const string ReadReceipt = "read_receipt";
    public const string Ping= "ping";

    // server -> client 
    public const string NewMessage = "new_message";
    public const string UserOnline= "user_online";
    public const string UserOffline= "user_offline";
    public const string TypingIndicator = "typing_indicator";
    public const string MessageRead = "message_read";
    public const string Pong = "pong";
    public const string Error = "error";

}
