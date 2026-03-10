namespace ChatApp.Application.DTOs.WebSocket;

public static class WebSocketMessageTypes
{
    //  Client → Server
    public const string SendMessage = "send_message"; 
    public const string Typing = "typing";            
    public const string ReadReceipt = "read_receipt"; 

    //Server → Client 

    public const string NewMessage = "new_message";         
    public const string MessageSent = "message_sent";        
    public const string UserOnline = "user_online";          
    public const string UserOffline = "user_offline";        
    public const string TypingIndicator = "typing_indicator"; 
    public const string MessageRead = "message_read";        
    public const string MessageDelivered = "message_delivered"; 
    public const string Error = "error";                     
}