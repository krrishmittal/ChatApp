namespace ChatApp.Application.Interfaces.Services;
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string fullName);
    Task SendLoginNotificationEmailAsync(string toEmail, string fullName);
    Task SendOtpEmailAsync(string toEmail, string fullname, string otpCode);
}
