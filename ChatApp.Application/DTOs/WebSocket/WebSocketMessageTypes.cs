namespace ChatApp.Application.DTOs.WebSocket;

public static class WebSocketMessageTypes
{
    // ─── Client → Server ─────────────────────────────
    // Frontend sends these TO our server
    public const string SendMessage = "send_message";   // user sends a message
    public const string Typing = "typing";              // user is typing
    public const string ReadReceipt = "read_receipt";  // user read a message

    // ─── Server → Client ─────────────────────────────
    // Our server sends these TO frontend
    public const string NewMessage = "new_message";           // new message arrived
    public const string MessageSent = "message_sent";         // confirmation to sender
    public const string UserOnline = "user_online";           // someone came online
    public const string UserOffline = "user_offline";         // someone went offline
    public const string TypingIndicator = "typing_indicator"; // someone is typing
    public const string MessageRead = "message_read";         // message was read
    public const string Error = "error";                      // something went wrong
}