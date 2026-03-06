using ChatApp.Infrastructure.WebSockets;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatApp.API.Middleware;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebSocketHandler _webSocketHandler;
    private readonly IConfiguration _config;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(
        RequestDelegate next,
        WebSocketHandler webSocketHandler,
        IConfiguration config,
        ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _webSocketHandler = webSocketHandler;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only handle /ws WebSocket requests
        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
        {
            // 1. Extract token from query string
            var token = context.Request.Query["token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("WebSocket connection rejected: missing token");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing token.");
                return;
            }

            // 2. Validate token and extract userId
            var userId = ValidateToken(token);
            if (userId is null)
            {
                _logger.LogWarning("WebSocket connection rejected: invalid token");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired token.");
                return;
            }

            // 3. Accept the WebSocket connection
            _logger.LogInformation("WebSocket connection accepted for UserId={UserId}", userId);
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            // 4. Hand off to WebSocketHandler
            await _webSocketHandler.HandleAsync(userId.Value, socket);
        }
        else
        {
            // Not a WebSocket request — pass to next middleware (normal HTTP)
            await _next(context);
        }
    }

    private Guid? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero 
            }, out _);

            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}