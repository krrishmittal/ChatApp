using ChatApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace ChatApp.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SmtpClient CreateSmtpClient()
    {
        var settings = _config.GetSection("EmailSettings");
        return new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new System.Net.NetworkCredential(settings["SenderEmail"], settings["Password"]),
            EnableSsl = true
        };
    }
    
    private string LoadTemplate(string templateName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", templateName);
        return File.ReadAllText(path);
    }
    public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
    {
        try
        {
            var settings = _config.GetSection("EmailSettings");
            var body = LoadTemplate("WelcomeTemplate.html")
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{CURRENT_YEAR}}", DateTime.Now.Year.ToString());

            var mail = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"]!, settings["FromName"]),
                Subject = "Welcome to ChatApp 💬",
                IsBodyHtml = true,
                Body = body
            };
            mail.To.Add(toEmail);

            using var smtp = CreateSmtpClient();
            await smtp.SendMailAsync(mail);

            _logger.LogInformation("Welcome email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendLoginNotificationEmailAsync(string toEmail, string fullName)
    {
        try
        {
            var settings = _config.GetSection("EmailSettings");
            var body = LoadTemplate("LoginTemplate.html")
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{LOGIN_TIME}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC")
                .Replace("{{CURRENT_YEAR}}", DateTime.Now.Year.ToString());

            var mail = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"]!, settings["FromName"]),
                Subject = "New Login to Your ChatApp Account 🔐",
                IsBodyHtml = true,
                Body = body
            };
            mail.To.Add(toEmail);

            using var smtp = CreateSmtpClient();
            await smtp.SendMailAsync(mail);

            _logger.LogInformation("Login notification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login notification email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        try
        {
            var settings = _config.GetSection("EmailSettings");
            var body = LoadTemplate("OtpTemplate.html")
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{OTP_CODE}}", otpCode)
                .Replace("{{EXPIRY_MINUTES}}", _config["OtpSettings:ExpiryMinutes"] ?? "10")
                .Replace("{{CURRENT_YEAR}}", DateTime.Now.Year.ToString());

            var mail = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"]!, settings["FromName"]),
                Subject = "Your Password Reset OTP 🔑",
                IsBodyHtml = true,
                Body = body
            };
            mail.To.Add(toEmail);

            using var smtp = CreateSmtpClient();
            await smtp.SendMailAsync(mail);

            _logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendPasswordResetSuccessEmailAsync(string toEmail, string fullName)
    {
        try
        {
            var settings = _config.GetSection("EmailSettings");
            var body = LoadTemplate("PasswordResetSuccessTemplate.html")
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{RESET_TIME}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC")
                .Replace("{{CURRENT_YEAR}}", DateTime.Now.Year.ToString());

            var mail = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"]!, settings["FromName"]),
                Subject = "Password Reset Successful ✅",
                IsBodyHtml = true,
                Body = body
            };
            mail.To.Add(toEmail);

            using var smtp = CreateSmtpClient();
            await smtp.SendMailAsync(mail);

            _logger.LogInformation("Password reset success email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset success email to {Email}", toEmail);
            throw;
        }
    }
}