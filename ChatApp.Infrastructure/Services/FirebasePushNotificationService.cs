using ChatApp.Application.Interfaces.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace ChatApp.Infrastructure.Services;
public class FirebasePushNotificationService : IPushNotificationService
{
    private readonly ILogger<FirebasePushNotificationService> _logger;
    public FirebasePushNotificationService(IConfiguration config, ILogger<FirebasePushNotificationService> logger)
    {
        _logger = logger;
        if(FirebaseApp.DefaultInstance is null)
        {
            var credentialsPath = config["Firebase:CredentialsPath"];
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });
            _logger.LogInformation("Firebase app initialized successfully.");
        }
    }
    public async Task SendPushNotificationAsync(string fcmToken, string senderName, string messageContent,Guid conversationId)
    {
        try
        {
            var previewMessage = messageContent.Length > 50 ? messageContent[..50] + "..." : messageContent;
            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = $"New message from {senderName}",
                    Body = previewMessage
                },
                Data = new Dictionary<string, string>()
                {
                    {"type","new_message" },
                    { "conversationId", conversationId.ToString() },
                    {"senderName",senderName}
                },
                Webpush = new WebpushConfig()
                {
                    Notification=new WebpushNotification()
                    {
                        Title= $"New message from {senderName}",
                        Body = previewMessage,
                        Icon= "https://example.com/icons/message.png"
                    },
                    FcmOptions=new WebpushFcmOptions()
                    {
                        Link = $"/chat/{conversationId}"
                    }
                }
            };
            var response= await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Push notification sent successfully: {Result}", response);
        }
        catch (FirebaseMessagingException ex)
        when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            // Token is invalid/expired — should remove from DB
            _logger.LogWarning(
                "FCM token is invalid/unregistered for token={Token}", fcmToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notification");
        }
    }

    public async Task SendMessageReadNotificationAsync(string fcmToken, string readerName, Guid conversationId)
    {
        try
        {
            var message = new Message()
            {
                Token = fcmToken,
                Data = new Dictionary<string, string>()
                {
                    {"type","message_read" },
                    { "conversationId", conversationId.ToString() },
                    {"readerName",readerName}
                },
                Webpush = new WebpushConfig()
                {
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = $"/chat/{conversationId}"
                    }
                }
            };
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Message read notification sent successfully: {Result}", response);

        }
        catch (FirebaseMessagingException ex)
        when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            // Token is invalid/expired — should remove from DB
            _logger.LogWarning(
                "FCM token is invalid/unregistered for token={Token}", fcmToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message read notification");
        }
    }
}
