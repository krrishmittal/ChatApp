namespace ChatApp.Application.Interfaces.Services;
public interface ICaptchaService
{
    Task<bool> ValidateAsync(string token);
}