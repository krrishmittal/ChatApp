namespace ChatApp.Api.Middleware;
public class RateLimitResponseMiddleware
{
    private readonly RequestDelegate _next;
    public RateLimitResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == 429)
        { 
            if (context.Response.HasStarted) return;

            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Too many requests. Please try again later.",
                statusCode = 429,
                errors = (object?)null
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            await context.Response.WriteAsync(json); 
        }
    }
}
