namespace ChatApp.Application.Interfaces.Services;
public interface IPushNotificationService
{
    Task SendPushNotificationAsync(string fcmToken, string senderName, string message,Guid conversationId);
    Task SendMessageReadNotificationAsync(
        string fcmToken,
        string readerName,
        Guid conversationId);
}
