using ChatApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatApp.Infrastructure.Services;
public class CaptchaService : ICaptchaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<CaptchaService> _logger;
    public CaptchaService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<CaptchaService> logger,IHostEnvironment env)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _env = env;
        _logger = logger;
    }
    public async Task<bool>ValidateAsync(string token)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Captcha validation skipped in Development environment");
            return true;
        }
        try
        {
            var secretKey= _config["GoogleReCaptcha:SecretKey"];
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}", null);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            var success =result.GetProperty("success").GetBoolean();
            _logger.LogInformation("Captcha validation result: {Success}", success);
            return success;
        }
        catch (Exception ex)    
        {
            _logger.LogError(ex, "Error validating captcha");
            return false;
        }
    }
}
